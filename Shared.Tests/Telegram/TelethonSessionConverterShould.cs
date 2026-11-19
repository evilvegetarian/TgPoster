using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Shouldly;
using TgPoster.Telegram.Internal;

namespace Shared.Tests.Telegram;

public sealed class TelethonSessionConverterShould
{
	private const string ApiHash = "00112233445566778899AABBCCDDEEFF";

	[Fact]
	public void RecognizeSqliteHeader_WithoutAcceptingOtherFormats()
	{
		TelethonSessionConverter.IsTelethonSession("SQLite format 3\0payload"u8.ToArray()).ShouldBeTrue();
		TelethonSessionConverter.IsTelethonSession([]).ShouldBeFalse();
		TelethonSessionConverter.IsTelethonSession("SQLite format 4\0"u8.ToArray()).ShouldBeFalse();
	}

	[Theory]
	[InlineData(2, 3, 2)]
	[InlineData(4, 1, 1)]
	public void ConvertKeysIntoAuthenticatedEncryptedSession(int firstDc, int secondDc, int mainDc)
	{
		var first = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
		var second = Enumerable.Repeat((byte)42, 256).ToArray();
		var data = CreateSession((firstDc, first), (secondDc, second), (9, new byte[10]));

		using var result = TelethonSessionConverter.ConvertToWTelegramSession(data, "123", ApiHash);
		var bytes = result.ToArray();
		using var aes = Aes.Create();
		aes.Key = Convert.FromHexString(ApiHash);
		aes.IV = bytes[..16];
		var plaintext = aes.DecryptCbc(bytes[16..], aes.IV);
		var payload = plaintext[32..];
		using var json = JsonDocument.Parse(payload);

		result.Position.ShouldBe(0);
		plaintext[..32].ShouldBe(SHA256.HashData(payload));
		json.RootElement.GetProperty("ApiId").GetInt64().ShouldBe(123);
		json.RootElement.GetProperty("MainDC").GetInt32().ShouldBe(mainDc);
		var sessions = json.RootElement.GetProperty("DCSessions");
		sessions.EnumerateObject().Count().ShouldBe(2);
		sessions.GetProperty(firstDc.ToString()).GetProperty("AuthKey").GetBytesFromBase64().ShouldBe(first);
		sessions.GetProperty(secondDc.ToString()).GetProperty("AuthKey").GetBytesFromBase64().ShouldBe(second);
	}

	[Fact]
	public void RejectSessionWithoutValidAuthKeys()
	{
		var data = CreateSession((1, new byte[255]));

		Should.Throw<InvalidOperationException>(() => TelethonSessionConverter.ConvertToWTelegramSession(data, "123", ApiHash));
	}

	[Fact]
	public void RejectMalformedSqliteData()
	{
		Should.Throw<SqliteException>(() => TelethonSessionConverter.ConvertToWTelegramSession([1, 2, 3], "123", ApiHash));
	}

	private static byte[] CreateSession(params (int Dc, byte[] Key)[] sessions)
	{
		var path = Path.GetTempFileName();
		try
		{
			using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
			{
				connection.Open();
				using var schema = connection.CreateCommand();
				schema.CommandText = "CREATE TABLE sessions (dc_id INTEGER, auth_key BLOB)";
				schema.ExecuteNonQuery();
				foreach (var (dc, key) in sessions)
				{
					using var insert = connection.CreateCommand();
					insert.CommandText = "INSERT INTO sessions VALUES ($dc, $key)";
					insert.Parameters.AddWithValue("$dc", dc);
					insert.Parameters.AddWithValue("$key", key);
					insert.ExecuteNonQuery();
				}
			}
			return File.ReadAllBytes(path);
		}
		finally { File.Delete(path); }
	}
}

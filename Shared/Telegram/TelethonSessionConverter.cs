using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace Shared.Telegram;

/// <summary>
///     Конвертер сессий Telethon (.session SQLite) в формат WTelegram.
/// </summary>
internal static class TelethonSessionConverter
{
	private static readonly byte[] SqliteMagic = "SQLite format 3\0"u8.ToArray();

	/// <summary>
	///     Проверяет, является ли файл SQLite-сессией Telethon.
	/// </summary>
	public static bool IsTelethonSession(byte[] data)
	{
		if (data.Length < SqliteMagic.Length)
			return false;

		return data.AsSpan(0, SqliteMagic.Length).SequenceEqual(SqliteMagic);
	}

	/// <summary>
	///     Конвертирует Telethon SQLite-сессию в бинарный формат WTelegram.
	/// </summary>
	public static MemoryStream ConvertToWTelegramSession(byte[] telethonData, string apiId, string apiHash)
	{
		var dcSessions = ReadTelethonSessions(telethonData);

		if (dcSessions.Count == 0)
			throw new InvalidOperationException("Telethon-сессия не содержит авторизационных ключей.");

		var mainDc = dcSessions.ContainsKey(2) ? 2 : dcSessions.Keys.Min();

		var session = new WTelegramSessionDto
		{
			ApiId = long.Parse(apiId),
			MainDC = mainDc,
			DCSessions = dcSessions.ToDictionary(
				kv => kv.Key,
				kv => new DCSessionDto { AuthKey = kv.Value })
		};

		var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(session, JsonOptions);
		return EncryptSession(jsonBytes, apiHash);
	}

	private static Dictionary<int, byte[]> ReadTelethonSessions(byte[] sqliteData)
	{
		var tempFile = Path.GetTempFileName();

			File.WriteAllBytes(tempFile, sqliteData);

			var connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = tempFile,
				Mode = SqliteOpenMode.ReadOnly
			}.ToString();

			using var connection = new SqliteConnection(connectionString);
			connection.Open();

			using var command = connection.CreateCommand();
			command.CommandText = "SELECT dc_id, auth_key FROM sessions WHERE auth_key IS NOT NULL";

			var result = new Dictionary<int, byte[]>();
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				var dcId = reader.GetInt32(0);
				var authKey = (byte[])reader[1];

				if (authKey.Length == 256)
					result[dcId] = authKey;
			}

			return result;
		// finally
		// {
		// 	File.Delete(tempFile);
		// }
	}

	private static MemoryStream EncryptSession(byte[] jsonBytes, string apiHash)
	{
		var key = Convert.FromHexString(apiHash);
		var iv = RandomNumberGenerator.GetBytes(16);
		var hash = SHA256.HashData(jsonBytes);

		// Формат: SHA256(json) + json
		var plaintext = new byte[hash.Length + jsonBytes.Length];
		hash.CopyTo(plaintext, 0);
		jsonBytes.CopyTo(plaintext, hash.Length);

		using var aes = Aes.Create();
		aes.Key = key;
		aes.IV = iv;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;

		var encrypted = aes.CreateEncryptor().TransformFinalBlock(plaintext, 0, plaintext.Length);

		// Формат файла: IV(16) + encrypted_data
		var result = new MemoryStream();
		result.Write(iv);
		result.Write(encrypted);
		result.Position = 0;

		return result;
	}

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		IncludeFields = true,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
	};

	/// <summary>
	///     DTO, повторяющий структуру WTelegram.Session для JSON-сериализации.
	/// </summary>
	private sealed class WTelegramSessionDto
	{
		public long ApiId;
		public long UserId;
		public int MainDC;
		public Dictionary<int, DCSessionDto> DCSessions = [];
	}

	/// <summary>
	///     DTO, повторяющий структуру WTelegram.Session.DCSession для JSON-сериализации.
	/// </summary>
	private sealed class DCSessionDto
	{
		public byte[]? AuthKey;
		public long UserId;
		public long OldSalt;
		public long Salt;
	}
}

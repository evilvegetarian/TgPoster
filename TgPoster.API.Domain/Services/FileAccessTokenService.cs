using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TgPoster.API.Domain.ConfigModels;

namespace TgPoster.API.Domain.Services;

internal sealed class FileAccessTokenService(TelegramOptions options, TimeProvider timeProvider)
{
	public string Create(Guid fileId)
	{
		var expires = timeProvider.GetUtcNow().AddMinutes(10).ToUnixTimeSeconds();
		return $"{expires}.{Convert.ToBase64String(Sign(fileId, expires))}";
	}

	public bool IsValid(Guid fileId, string? token)
	{
		if (token is null || token.Length > 100)
			return false;
		var parts = token.Split('.');
		if (parts.Length != 2 || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var expires)
		    || expires <= timeProvider.GetUtcNow().ToUnixTimeSeconds())
			return false;

		Span<byte> signature = stackalloc byte[32];
		return Convert.TryFromBase64String(parts[1], signature, out var length) && length == signature.Length
			&& CryptographicOperations.FixedTimeEquals(signature, Sign(fileId, expires));
	}

	private byte[] Sign(Guid fileId, long expires) =>
		HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.SecretKey),
			Encoding.UTF8.GetBytes(FormattableString.Invariant($"file:{fileId:D}:{expires}")));
}

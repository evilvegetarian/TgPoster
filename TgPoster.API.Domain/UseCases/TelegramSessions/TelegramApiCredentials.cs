using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.TelegramSessions;

/// <summary>
///     API-ключи приложения Telegram для переиспользования между сессиями
/// </summary>
/// <param name="ApiId"></param>
/// <param name="ApiHash"></param>
public sealed record TelegramApiCredentials(string ApiId, string ApiHash)
{
	internal static async Task<TelegramApiCredentials> ResolveAsync(
		Guid? credentialsSessionId,
		string? apiId,
		string? apiHash,
		Func<Guid, Task<TelegramApiCredentials?>> getBySessionIdAsync
	)
	{
		if (credentialsSessionId.HasValue)
		{
			var credentials = await getBySessionIdAsync(credentialsSessionId.Value);
			if (credentials is null)
				throw new TelegramSessionEntityNotFoundException(credentialsSessionId.Value);

			if (string.IsNullOrWhiteSpace(credentials.ApiId) || string.IsNullOrWhiteSpace(credentials.ApiHash))
				throw new TelegramSessionCorruptedException(credentialsSessionId.Value);

			return credentials;
		}

		if (string.IsNullOrWhiteSpace(apiId) || string.IsNullOrWhiteSpace(apiHash))
			throw new InvalidTelegramApiCredentialsException();

		return new TelegramApiCredentials(apiId.Trim(), apiHash.Trim());
	}
}

using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на создание Telegram сессии
/// </summary>
/// <param name="ApiId">API ID приложения Telegram (опционально, если указан CredentialsSessionId)</param>
/// <param name="ApiHash">API Hash приложения Telegram (опционально, если указан CredentialsSessionId)</param>
/// <param name="PhoneNumber">Номер телефона</param>
/// <param name="Name">Название сессии (опционально)</param>
/// <param name="ProxyId">ID прокси для маршрутизации трафика сессии (опционально)</param>
/// <param name="NotificationBotId">ID бота, который оповещает о проблемах с аккаунтом (опционально)</param>
/// <param name="CredentialsSessionId">ID существующей сессии для переиспользования API ID и API Hash (опционально)</param>
public sealed record CreateTelegramSessionRequest(
	string? ApiId,
	string? ApiHash,
	[Required] string PhoneNumber,
	string? Name,
	Guid? ProxyId,
	Guid? NotificationBotId,
	Guid? CredentialsSessionId = null
) : IValidatableObject
{
	/// <inheritdoc />
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (!CredentialsSessionId.HasValue
		    && (string.IsNullOrWhiteSpace(ApiId) || string.IsNullOrWhiteSpace(ApiHash)))
		{
			yield return new ValidationResult(
				"Укажите API ID и API Hash либо выберите существующую сессию с API-ключами",
				[nameof(ApiId), nameof(ApiHash)]);
		}
	}
}

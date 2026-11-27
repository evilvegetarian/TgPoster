using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на импорт Telegram сессии из файла
/// </summary>
public sealed class ImportTelegramSessionRequest : IValidatableObject
{
	/// <summary>
	///     API ID приложения Telegram (опционально, если указан CredentialsSessionId)
	/// </summary>
	public string? ApiId { get; set; }

	/// <summary>
	///     API Hash приложения Telegram (опционально, если указан CredentialsSessionId)
	/// </summary>
	public string? ApiHash { get; set; }

	/// <summary>
	///     Файл сессии WTelegram (.session)
	/// </summary>
	[Required]
	public required IFormFile SessionFile { get; set; }

	/// <summary>
	///     Название сессии (опционально)
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	///     ID бота для оповещений о проблемах с аккаунтом (опционально)
	/// </summary>
	public Guid? NotificationBotId { get; set; }

	/// <summary>
	///     ID существующей сессии для переиспользования API ID и API Hash (опционально)
	/// </summary>
	public Guid? CredentialsSessionId { get; set; }

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

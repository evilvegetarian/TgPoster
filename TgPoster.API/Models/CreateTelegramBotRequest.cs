using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на создание Telegram бота
/// </summary>
public class CreateTelegramBotRequest : IValidatableObject
{
	/// <summary>
	///     Токен телеграм бота
	/// </summary>
	public required string Token { get; set; }

	/// <summary>
	///     Валидация запроса на создание Telegram бота
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationErrors = new List<ValidationResult>();

		if (Token.Length < 1 || string.IsNullOrWhiteSpace(Token))
		{
			validationErrors.Add(new ValidationResult("Токен необходим", [nameof(Token)]));
		}

		return validationErrors;
	}
}
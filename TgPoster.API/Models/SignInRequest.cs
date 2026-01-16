using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Регистрация
/// </summary>
public class SignInRequest : IValidatableObject
{
	/// <summary>
	/// Логин пользователя
	/// </summary>
	[MinLength(5)]
	[MaxLength(30)]
	public required string Login { get; set; }

	/// <summary>
	/// Пароль пользователя
	/// </summary>
	[MinLength(5)]
	public required string Password { get; set; }

	/// <summary>
	/// Валидация запроса на регистрацию
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationResults = new List<ValidationResult>();

		if (string.IsNullOrWhiteSpace(Login))
		{
			validationResults.Add(new ValidationResult("Логин должен не быть пустым",
				[nameof(Login)]));
		}

		if (string.IsNullOrWhiteSpace(Password))
		{
			validationResults.Add(new ValidationResult("Пароль должен не быть пустым",
				[nameof(Password)]));
		}

		return validationResults;
	}
}
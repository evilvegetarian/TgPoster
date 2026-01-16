using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
/// Запрос на авторизацию YouTube
/// </summary>
public class LoginYouTubeRequest : IValidatableObject
{
	/// <summary>
	/// JSON файл с учетными данными
	/// </summary>
	public IFormFile? JsonFile { get; set; }

	/// <summary>
	/// Идентификатор клиента
	/// </summary>
	public string? ClientId { get; set; }

	/// <summary>
	/// Секретный ключ клиента
	/// </summary>
	public string? ClientSecret { get; set; }

	/// <summary>
	/// Валидация запроса на авторизацию YouTube
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationErrors = new List<ValidationResult>();

		if (JsonFile is null && ClientId is not null && ClientSecret is not null)
		{
			var errorMessage = "Если файл пустой, должны быть введены ClientId и ClientSecret.";
			validationErrors.Add(new ValidationResult(
				errorMessage,
				[nameof(JsonFile), nameof(ClientId), nameof(ClientSecret)]
			));
		}

		return validationErrors;
	}
}
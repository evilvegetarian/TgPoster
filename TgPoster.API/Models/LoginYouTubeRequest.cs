using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

public class LoginYouTubeRequest : IValidatableObject
{
	public IFormFile? JsonFile { get; set; }
	public string? ClientId { get; set; }
	public string? ClientSecret { get; set; }

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
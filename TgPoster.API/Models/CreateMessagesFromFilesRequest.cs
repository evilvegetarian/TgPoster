using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
/// Запрос на создание сообщений из файлов
/// </summary>
public class CreateMessagesFromFilesRequest : IValidatableObject
{
	/// <summary>
	/// Идентификатор расписания
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	/// Список файлов для создания сообщений
	/// </summary>
	public required List<IFormFile> Files { get; set; } = [];

	/// <summary>
	/// Валидация запроса на создание сообщений из файлов
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationErrors = new List<ValidationResult>();

		if (!Files.Any())
		{
			validationErrors.Add(new ValidationResult("Files are required", [nameof(Files)]));
		}

		return validationErrors;
	}
}
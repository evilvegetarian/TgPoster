using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на редактирование сообщения
/// </summary>
public sealed class EditMessageRequest : IValidatableObject
{
	/// <summary>
	///     Id расписания
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     Время поста
	/// </summary>
	public required DateTimeOffset TimePosting { get; set; }

	/// <summary>
	///     Текстовое сообщение
	/// </summary>
	public string? TextMessage { get; set; }

	/// <summary>
	///     Старые файлы сообщения
	/// </summary>
	public List<Guid> OldFiles { get; set; } = [];

	/// <summary>
	///     Новые файлы сообщения
	/// </summary>
	public List<IFormFile> NewFiles { get; set; } = [];

	/// <summary>
	///     Кросс-постить ли пост; null — не менять
	/// </summary>
	public bool? CrossPostEnabled { get; set; }

	/// <summary>
	///     Формат кросс-поста; null — не менять, Inherit — как в настройках расписания
	/// </summary>
	public MessageCrossPostFormat? CrossPostFormat { get; set; }

	/// <summary>
	///     Валидация запроса на редактирование сообщения
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationErrors = new List<ValidationResult>();
		if (DateTimeOffset.UtcNow > TimePosting)
		{
			validationErrors.Add(new ValidationResult(
				"Текущее время больше времени поста",
				[nameof(TimePosting)]
			));
		}

		return validationErrors;
	}
}
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на создание сообщения
/// </summary>
public sealed class CreateMessageRequest : IValidatableObject
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
	///     Файлы сообщения
	/// </summary>
	public List<IFormFile> Files { get; set; } = [];

	/// <summary>
	///     Кросс-постить ли пост в подключённые соцсети. По умолчанию — да
	/// </summary>
	[DefaultValue(true)]
	public bool? CrossPostEnabled { get; set; }

	/// <summary>
	///     Формат кросс-поста; Inherit или пусто — как в настройках расписания
	/// </summary>
	public MessageCrossPostFormat? CrossPostFormat { get; set; }

	/// <summary>
	///     Валидация запроса на создание сообщения
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
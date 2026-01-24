using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Создание настроек репоста для расписания.
/// </summary>
public sealed class CreateRepostSettingsRequest : IValidatableObject
{
	/// <summary>
	///     ID расписания, для которого создаются настройки репоста.
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     ID Telegram сессии для выполнения репостов.
	/// </summary>
	public required Guid TelegramSessionId { get; set; }

	/// <summary>
	///     Список целевых каналов/чатов для репоста (ID или @username).
	/// </summary>
	public List<string> Destinations { get; set; } = [];

	/// <summary>
	///     Валидация запроса.
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationResults = new List<ValidationResult>();

		if (ScheduleId == Guid.Empty)
		{
			validationResults.Add(new ValidationResult("Необходимо указать ScheduleId", [nameof(ScheduleId)]));
		}

		if (TelegramSessionId == Guid.Empty)
		{
			validationResults.Add(new ValidationResult("Необходимо указать TelegramSessionId",
				[nameof(TelegramSessionId)]));
		}

		return validationResults;
	}
}

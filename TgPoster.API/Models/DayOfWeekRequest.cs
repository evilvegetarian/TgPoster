using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Настройки публикации для дня недели
/// </summary>
public class DayOfWeekRequest : IValidatableObject
{
	/// <summary>
	///     День недели
	/// </summary>
	public required DayOfWeek DayOfWeekPosting { get; set; }

	/// <summary>
	///     Время начала
	/// </summary>
	public required TimeOnly StartPosting { get; set; }

	/// <summary>
	///     Время окончания
	/// </summary>
	public required TimeOnly EndPosting { get; set; }

	/// <summary>
	///     Интервал в минутах между постами
	/// </summary>
	[Range(1, 1440, ErrorMessage = "Интервал должен быть между 1 и 1440.")]
	public required byte Interval { get; set; }

	/// <summary>
	///     Валидация настроек публикации для дня недели
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationErrors = new List<ValidationResult>();

		if (EndPosting <= StartPosting)
		{
			validationErrors.Add(new ValidationResult(
				"EndPosting должно быть больше, чем StartPosting.",
				[nameof(EndPosting), nameof(StartPosting)]
			));
		}

		if ((EndPosting - StartPosting).TotalMinutes < Interval)
		{
			validationErrors.Add(new ValidationResult(
				"Интервал должен быть меньше чем разница старта и начала",
				[nameof(EndPosting), nameof(StartPosting), nameof(Interval)]
			));
		}

		return validationErrors;
	}
}
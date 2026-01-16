using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
/// Запрос на создание дней недели с настройками публикации
/// </summary>
public class CreateDaysRequest : IValidatableObject
{
	/// <summary>
	///     Id расписания
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     Список дней недели с настройками публикации
	/// </summary>
	public List<DayOfWeekRequest> DaysOfWeek { get; set; } = [];

	/// <summary>
	/// Валидация запроса на создание дней недели
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationErrors = new List<ValidationResult>();

		var duplicateDays = DaysOfWeek
			.GroupBy(x => x.DayOfWeekPosting)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToList();

		if (duplicateDays.Any())
		{
			var duplicateDaysNames = duplicateDays
				.Select(day => day.ToString())
				.ToArray();

			var errorMessage = $"Дублирующиеся дни недели: {string.Join(", ", duplicateDaysNames)}.";

			validationErrors.Add(new ValidationResult(
				errorMessage,
				[nameof(DaysOfWeek)]
			));
		}

		if (DaysOfWeek.Count > 7)
		{
			validationErrors.Add(new ValidationResult(
				"Количество дней недели не может превышать 7.",
				[nameof(DaysOfWeek)]
			));
		}

		return validationErrors;
	}
}
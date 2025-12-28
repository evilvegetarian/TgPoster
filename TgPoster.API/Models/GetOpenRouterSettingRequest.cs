using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос получения Open Router
/// </summary>
public class GetOpenRouterSettingRequest : IValidatableObject
{
	/// <summary>
	///     Id Open Router
	/// </summary>
	public Guid? Id { get; set; }

	/// <summary>
	///     Id расписания
	/// </summary>
	public Guid? ScheduleId { get; set; }

	/// <summary>
	///     Валидация GetOpenRouterSettingRequest
	/// </summary>
	/// <param name="validationContext"></param>
	/// <returns></returns>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var validationErrors = new List<ValidationResult>();
		if (!Id.HasValue && !ScheduleId.HasValue)
		{
			validationErrors.Add(new ValidationResult("Необходим либо расписание, либо id open router"));
		}

		return validationErrors;
	}
}
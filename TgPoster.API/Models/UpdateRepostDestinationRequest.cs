using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Обновление целевого канала репоста.
/// </summary>
public sealed class UpdateRepostDestinationRequest : IValidatableObject
{
	/// <summary>
	///     Активен ли целевой канал.
	/// </summary>
	[Required]
	public required bool IsActive { get; set; }

	/// <summary>
	///     Минимальная задержка перед репостом (секунды).
	/// </summary>
	[Range(0, int.MaxValue)]
	public int DelayMinSeconds { get; set; }

	/// <summary>
	///     Максимальная задержка перед репостом (секунды).
	/// </summary>
	[Range(0, int.MaxValue)]
	public int DelayMaxSeconds { get; set; }

	/// <summary>
	///     Репостить каждое N-е сообщение (1 = каждое).
	/// </summary>
	[Range(1, int.MaxValue)]
	[DefaultValue(1)]
	public int RepostEveryNth { get; set; } = 1;

	/// <summary>
	///     Вероятность пропуска репоста (0-100%).
	/// </summary>
	[Range(0, 100)]
	public int SkipProbability { get; set; }

	/// <summary>
	///     Максимальное количество репостов в день (null = без лимита).
	/// </summary>
	[Range(1, int.MaxValue)]
	public int? MaxRepostsPerDay { get; set; }

	/// <summary>
	///     Кросс-полевая валидация диапазона задержек
	/// </summary>
	/// <param name="validationContext">Контекст валидации</param>
	/// <returns>Ошибки валидации, если диапазон задан некоректно</returns>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (DelayMaxSeconds < DelayMinSeconds)
		{
			yield return new ValidationResult(
				"DelayMaxSeconds не может быть меньше DelayMinSeconds",
				[nameof(DelayMaxSeconds)]);
		}
	}
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Обновление настроек репоста.
/// </summary>
public sealed class UpdateRepostSettingsRequest : IValidatableObject
{
	/// <summary>
	///     Активность настроек репоста.
	/// </summary>
	[Required]
	public required bool IsActive { get; set; }

	/// <summary>
	///     Общая минимальная задержка перед репостом (секунды), копируется на новые каналы
	/// </summary>
	[Range(0, int.MaxValue)]
	public int DefaultDelayMinSeconds { get; set; }

	/// <summary>
	///     Общая максимальная задержка перед репостом (секунды), копируется на новые каналы
	/// </summary>
	[Range(0, int.MaxValue)]
	public int DefaultDelayMaxSeconds { get; set; }

	/// <summary>
	///     Общая настройка "репостить каждое N-е сообщение" (1 = каждое), копируется на новые каналы
	/// </summary>
	[Range(1, int.MaxValue)]
	[DefaultValue(1)]
	public int DefaultRepostEveryNth { get; set; } = 1;

	/// <summary>
	///     Общая вероятность пропуска репоста (0-100%), копируется на новые каналы
	/// </summary>
	[Range(0, 100)]
	public int DefaultSkipProbability { get; set; }

	/// <summary>
	///     Общий лимит репостов в день (null = без лимита), копируется на новые каналы
	/// </summary>
	[Range(1, int.MaxValue)]
	public int? DefaultMaxRepostsPerDay { get; set; }

	/// <summary>
	///     Кросс-полевая валидация диапазона задержек
	/// </summary>
	/// <param name="validationContext">Контекст валидации</param>
	/// <returns>Ошибки валидации, если диапазон задан некорректно</returns>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (DefaultDelayMaxSeconds < DefaultDelayMinSeconds)
		{
			yield return new ValidationResult(
				"DefaultDelayMaxSeconds не может быть меньше DefaultDelayMinSeconds",
				[nameof(DefaultDelayMaxSeconds)]);
		}
	}
}

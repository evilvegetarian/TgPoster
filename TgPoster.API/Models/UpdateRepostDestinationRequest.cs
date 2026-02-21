using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Обновление целевого канала репоста.
/// </summary>
public sealed class UpdateRepostDestinationRequest
{
	/// <summary>
	///     Активен ли целевой канал.
	/// </summary>
	[Required]
	public required bool IsActive { get; set; }

	/// <summary>
	///     Минимальная задержка перед репостом (секунды).
	/// </summary>
	public int DelayMinSeconds { get; set; }

	/// <summary>
	///     Максимальная задержка перед репостом (секунды).
	/// </summary>
	public int DelayMaxSeconds { get; set; }

	/// <summary>
	///     Репостить каждое N-е сообщение (1 = каждое).
	/// </summary>
	public int RepostEveryNth { get; set; } = 1;

	/// <summary>
	///     Вероятность пропуска репоста (0-100%).
	/// </summary>
	public int SkipProbability { get; set; }

	/// <summary>
	///     Максимальное количество репостов в день (null = без лимита).
	/// </summary>
	public int? MaxRepostsPerDay { get; set; }
}

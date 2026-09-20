using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос статистики по обнаруженным каналам
/// </summary>
public sealed class GetDiscoverStatsRequest
{
	/// <summary>
	///     Глубина таймлайнов «по дням» в днях, включая сегодня. По умолчанию — 30
	/// </summary>
	[Range(1, 365)]
	[DefaultValue(30)]
	public int Days { get; init; } = 30;
}

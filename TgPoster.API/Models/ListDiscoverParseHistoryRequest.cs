namespace TgPoster.API.Models;

/// <summary>
///     Запрос истории парсинга обнаруженных каналов
/// </summary>
public sealed class ListDiscoverParseHistoryRequest : PaginationRequest
{
	/// <summary>
	///     Поиск по названию или username
	/// </summary>
	public string? Search { get; init; }

	/// <summary>
	///     Начало периода по времени парсинга (включительно)
	/// </summary>
	public DateTimeOffset? From { get; init; }

	/// <summary>
	///     Конец периода по времени парсинга (включительно)
	/// </summary>
	public DateTimeOffset? To { get; init; }
}

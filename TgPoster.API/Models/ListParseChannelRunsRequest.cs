using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
/// Запрос журнала парсинга каналов
/// </summary>
public sealed class ListParseChannelRunsRequest : PaginationRequest
{
	/// <summary>
	/// Только проходы этого источника
	/// </summary>
	public Guid? ParseChannelId { get; init; }

	/// <summary>
	/// Только проходы с этим итогом
	/// </summary>
	public ParseChannelRunStatus? Status { get; init; }

	/// <summary>
	/// Начало периода по времени прохода (включительно)
	/// </summary>
	public DateTimeOffset? From { get; init; }

	/// <summary>
	/// Конец периода по времени прохода (включительно)
	/// </summary>
	public DateTimeOffset? To { get; init; }
}

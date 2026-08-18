using Shared.Enums;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос журнала репостов
/// </summary>
public sealed class ListRepostLogsRequest : PaginationRequest
{
	/// <summary>
	///     Показать только записи этих настроек репоста
	/// </summary>
	public Guid? RepostSettingsId { get; init; }

	/// <summary>
	///     Показать только записи по конкретному целевому каналу
	/// </summary>
	public Guid? DestinationId { get; init; }

	/// <summary>
	///     Показать только записи по конкретному сообщению
	/// </summary>
	public Guid? MessageId { get; init; }

	/// <summary>
	///     Фильтр по статусу репоста
	/// </summary>
	public RepostStatus? Status { get; init; }

	/// <summary>
	///     Начало периода (включительно)
	/// </summary>
	public DateTimeOffset? From { get; init; }

	/// <summary>
	///     Конец периода (включительно)
	/// </summary>
	public DateTimeOffset? To { get; init; }
}

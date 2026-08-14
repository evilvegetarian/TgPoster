using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;

/// <summary>
///     Состояние задания на массовое добавление каналов вместе с результатами по каждому каналу
/// </summary>
/// <param name="JobId">Id задания</param>
/// <param name="Status">Текущий статус задания</param>
/// <param name="SessionFloodWaitUntil">До какого момента Telegram ограничил сессию (null — ограничений нет)</param>
/// <param name="Items">Результаты по каждому каналу</param>
public sealed record RepostImportJobState(
	Guid JobId,
	RepostImportStatus Status,
	DateTimeOffset? SessionFloodWaitUntil,
	List<RepostImportJobItemState> Items);

/// <summary>
///     Результат обработки одного канала задания
/// </summary>
/// <param name="DiscoveredChannelId">Id записи в Discover</param>
/// <param name="Title">Название канала</param>
/// <param name="Outcome">Итог обработки</param>
/// <param name="RepostDestinationId">Id созданного целевого канала (только при Outcome = Added)</param>
/// <param name="Error">Причина отказа</param>
public sealed record RepostImportJobItemState(
	Guid DiscoveredChannelId,
	string Title,
	AddDestinationOutcome Outcome,
	Guid? RepostDestinationId,
	string? Error);

public interface IGetRepostImportJobStorage
{
	/// <summary>
	///     Получить состояние задания пользователя
	/// </summary>
	/// <param name="jobId">Id задания</param>
	/// <param name="userId">Id пользователя-владельца</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Состояние задания или null, если его нет или оно принадлежит другому пользователю</returns>
	Task<RepostImportJobState?> GetJobStateAsync(Guid jobId, Guid userId, CancellationToken ct);
}

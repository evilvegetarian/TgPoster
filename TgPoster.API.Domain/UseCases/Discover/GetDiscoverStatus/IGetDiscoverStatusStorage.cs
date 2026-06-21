namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

/// <summary>
///     Статус задачи, как он записан воркером в хранилище
/// </summary>
public enum WorkerJobStateStatus
{
	Idle = 0,
	Running = 1,
	CooldownWait = 2,
	Failed = 3
}

/// <summary>
///     Снимок состояния фоновой задачи из хранилища
/// </summary>
public sealed record WorkerJobStateDto(
	WorkerJobStateStatus Status,
	DateTimeOffset? LastStartedAt,
	DateTimeOffset? LastFinishedAt,
	DateTimeOffset? HeartbeatAt,
	DateTimeOffset? CooldownUntil,
	DateTimeOffset? NextRunAt,
	string? LastError,
	int? ProgressCurrent,
	int? ProgressTotal,
	string? ProgressMessage);

public interface IGetDiscoverStatusStorage
{
	/// <summary>
	///     Получить состояние задачи обнаружения каналов
	/// </summary>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Снимок состояния или null, если задача ещё не региструвалась</returns>
	Task<WorkerJobStateDto?> GetDiscoverJobStateAsync(CancellationToken ct);
}
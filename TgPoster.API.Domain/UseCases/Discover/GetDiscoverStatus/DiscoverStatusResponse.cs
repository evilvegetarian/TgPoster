namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

/// <summary>
///     Эффективный статус фоновой задачи обнаружения каналов
/// </summary>
public enum DiscoverJobStatus
{
	/// <summary>
	///     Не выполняется: ждёт следующего запуска по расписанию
	/// </summary>
	Idle,

	/// <summary>
	///     Выполняется
	/// </summary>
	Running,

	/// <summary>
	///     Ждёт окончания FloodWait-таймаута
	/// </summary>
	CooldownWait,

	/// <summary>
	///     Последний запуск завершился ошибкой
	/// </summary>
	Failed,

	/// <summary>
	///     Состояние неизвестно: воркер перестал подавать признаки жизни
	/// </summary>
	Unknown
}

/// <summary>
///     Состояние фоновой задачи обнаружения каналов
/// </summary>
public sealed record DiscoverStatusResponse
{
	/// <summary>Эффективный статус задачи.</summary>
	public required DiscoverJobStatus Status { get; init; }

	/// <summary>Время начала последнего запуска.</summary>
	public DateTimeOffset? LastStartedAt { get; init; }

	/// <summary>Время завершения последнего запуска.</summary>
	public DateTimeOffset? LastFinishedAt { get; init; }

	/// <summary>Время окончания FloodWait-таймаута (только при статусе CooldownWait).</summary>
	public DateTimeOffset? CooldownUntil { get; init; }

	/// <summary>Время следующего запуска по расписанию.</summary>
	public DateTimeOffset? NextRunAt { get; init; }

	/// <summary>Текст последней ошибки (только при статусе Failed).</summary>
	public string? LastError { get; init; }

	/// <summary>Сколько единиц работы обработано (только при статусе Running).</summary>
	public int? ProgressCurrent { get; init; }

	/// <summary>Сколько всего единиц работы (только при статусе Running).</summary>
	public int? ProgressTotal { get; init; }

	/// <summary>Описание текущего этапа (только при статусе Running).</summary>
	public string? ProgressMessage { get; init; }

	/// <summary>Время последнего сигнала жизни от воркера.</summary>
	public DateTimeOffset? HeartbeatAt { get; init; }
}
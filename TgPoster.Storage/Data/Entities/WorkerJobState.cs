using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Состояние фоновой задачи воркера
/// </summary>
public sealed class WorkerJobState : BaseEntity
{
	/// <summary>Имя job'а в Hangfire.</summary>
	public required string JobName { get; set; }

	/// <summary>Текущий статус задачи.</summary>
	public WorkerJobStatus Status { get; set; }

	/// <summary>Время начала последнего запуска.</summary>
	public DateTimeOffset? LastStartedAt { get; set; }

	/// <summary>Время завершения последнего запуска.</summary>
	public DateTimeOffset? LastFinishedAt { get; set; }

	/// <summary>Время последнего сигнала жизни от воркера.</summary>
	public DateTimeOffset? HeartbeatAt { get; set; }

	/// <summary>Время окончания FloodWait-таймаута.</summary>
	public DateTimeOffset? CooldownUntil { get; set; }

	/// <summary>Время следующего запуска по расписанию.</summary>
	public DateTimeOffset? NextRunAt { get; set; }

	/// <summary>Текст последней ошибки.</summary>
	public string? LastError { get; set; }

	/// <summary>Сколько единиц работы обработано в текущем запуске.</summary>
	public int? ProgressCurrent { get; set; }

	/// <summary>Сколько всего единиц работы в текущем запуске.</summary>
	public int? ProgressTotal { get; set; }

	/// <summary>Человекочитаемое описание текущего этапа.</summary>
	public string? ProgressMessage { get; set; }
}
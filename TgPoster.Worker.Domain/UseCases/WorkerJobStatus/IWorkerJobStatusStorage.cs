namespace TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

/// <summary>
///     Хранилище состояния фоновых задач воркера
/// </summary>
public interface IWorkerJobStatusStorage
{
	/// <summary>
	///     Создать запись о задаче, если её ещё нет. Если запись уже существует,
	///     статус не сбрасывается (воркер мог перезапуститься во время Running/CooldownWait) —
	///     обновляется только время следующего запуска
	/// </summary>
	/// <param name="jobName">Имя job'а в Hangfire</param>
	/// <param name="nextRunAt">Время следующего запуска по расписанию</param>
	/// <param name="ct">Токен отмены</param>
	Task EnsureRegisteredAsync(string jobName, DateTimeOffset? nextRunAt, CancellationToken ct);

	/// <summary>
	///     Зафиксировать начало запуска: статус Running, сброс ошибки, таймаута и прогресса
	/// </summary>
	/// <param name="jobName">Имя job'а в Hangfire</param>
	/// <param name="ct">Токен отмены</param>
	Task ReportStartedAsync(string jobName, CancellationToken ct);

	/// <summary>
	///     Обновить сигнал жизни и прогрес текущего запуска
	/// </summary>
	/// <param name="jobName">Имя job'а в Hangfire</param>
	/// <param name="current">Сколько единиц работы обработано</param>
	/// <param name="total">Сколько всего единиц работы в запуске</param>
	/// <param name="message">Человекочитаемое описание текущего этапа</param>
	/// <param name="ct">Токен отмены</param>
	Task ReportHeartbeatAsync(string jobName, int? current, int? total, string? message, CancellationToken ct);

	/// <summary>
	///     Зафиксировать FloodWait-таймаут: статус CooldownWait до указанного времени
	/// </summary>
	/// <param name="jobName">Имя job'а в Hangfire</param>
	/// <param name="cooldownUntil">Время окончания таймаута</param>
	/// <param name="nextRunAt">Время следующего запуска по расписанию</param>
	/// <param name="ct">Токен отмены</param>
	Task ReportCooldownAsync(
		string jobName,
		DateTimeOffset cooldownUntil,
		DateTimeOffset? nextRunAt,
		CancellationToken ct
	);

	/// <summary>
	///     Зафиксировать успешное завершение запуска: статус Idle
	/// </summary>
	/// <param name="jobName">Имя job'а в Hangfire</param>
	/// <param name="nextRunAt">Время следующего запуска по расписанию</param>
	/// <param name="ct">Токен отмены</param>
	Task ReportCompletedAsync(string jobName, DateTimeOffset? nextRunAt, CancellationToken ct);

	/// <summary>
	///     Зафиксировать завершение с ошибкой: статус Failed с текстом ошибки
	/// </summary>
	/// <param name="jobName">Имя job'а в Hangfire</param>
	/// <param name="error">Текст ошибки</param>
	/// <param name="nextRunAt">Время следующего запуска по расписанию</param>
	/// <param name="ct">Токен отмены</param>
	Task ReportFailedAsync(string jobName, string error, DateTimeOffset? nextRunAt, CancellationToken ct);
}
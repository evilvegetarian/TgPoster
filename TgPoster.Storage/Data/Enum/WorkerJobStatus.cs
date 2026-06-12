namespace TgPoster.Storage.Data.Enum;

/// <summary>
///     Статус фоновой задачи воркера
/// </summary>
public enum WorkerJobStatus
{
	/// <summary>
	///     Простаивает: ждёт следующего запуска или успешно завершилась
	/// </summary>
	Idle = 0,

	/// <summary>
	///     Выполняется
	/// </summary>
	Running = 1,

	/// <summary>
	///     Ждёт окончания FloodWait-таймаута
	/// </summary>
	CooldownWait = 2,

	/// <summary>
	///     Завершилась с ошибкой
	/// </summary>
	Failed = 3
}

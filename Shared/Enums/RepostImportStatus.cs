namespace Shared.Enums;

/// <summary>
///     Статус задания на массовое добавление целевых каналов из Discover
/// </summary>
public enum RepostImportStatus
{
	/// <summary>
	///     Задание создано и ждёт обработки
	/// </summary>
	Pending = 0,

	/// <summary>
	///     Каналы обрабатываются
	/// </summary>
	InProgress = 1,

	/// <summary>
	///     Обработка приостановлена: Telegram ограничил сессию
	/// </summary>
	CooldownWait = 2,

	/// <summary>
	///     Все каналы обработаны
	/// </summary>
	Completed = 3,

	/// <summary>
	///     Задание завершилось ошибкой и не будет продолжено
	/// </summary>
	Failed = 4
}

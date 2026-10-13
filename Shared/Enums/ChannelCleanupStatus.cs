namespace Shared.Enums;

/// <summary>
/// Статус задания на выход из неиспользуемых каналов и групп
/// </summary>
public enum ChannelCleanupStatus
{
	/// <summary>
	/// Задание создано и ждёт обработки
	/// </summary>
	Pending = 0,

	/// <summary>
	/// Аккаунт выходит из каналов
	/// </summary>
	InProgress = 1,

	/// <summary>
	/// Обработка приостановлена: Telegram ограничил сессию
	/// </summary>
	CooldownWait = 2,

	/// <summary>
	/// Все каналы обработаны
	/// </summary>
	Completed = 3,

	/// <summary>
	/// Задание остановлено с ошибкой и не будет продолжено
	/// </summary>
	Failed = 4
}

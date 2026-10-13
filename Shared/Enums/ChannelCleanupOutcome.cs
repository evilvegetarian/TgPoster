namespace Shared.Enums;

/// <summary>
/// Итог обработки одного канала или группы при очистке
/// </summary>
public enum ChannelCleanupOutcome
{
	/// <summary>
	/// Канал в очереди на выход
	/// </summary>
	Pending = 0,

	/// <summary>
	/// Аккаунт вышел из канала
	/// </summary>
	Left = 1,

	/// <summary>
	/// Аккаунт к моменту обработки уже не состоял в канале
	/// </summary>
	NotMember = 2,

	/// <summary>
	/// Пока задание ждало, канал начал использоваться приложением — выход отменён
	/// </summary>
	InUse = 3,

	/// <summary>
	/// Telegram не дал выйти из канала
	/// </summary>
	Failed = 4,

	/// <summary>
	/// Канал не обработан: задание остановлено и не будет продолжено
	/// </summary>
	NotProcessed = 5
}

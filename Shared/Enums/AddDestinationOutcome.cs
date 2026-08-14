namespace Shared.Enums;

/// <summary>
///     Итог обработки одного канала при массовом добавлении
/// </summary>
public enum AddDestinationOutcome
{
	/// <summary>
	///     Канал добавлен в список целевых
	/// </summary>
	Added = 0,

	/// <summary>
	///     Канал уже был добавлен ранее
	/// </summary>
	AlreadyAdded = 1,

	/// <summary>
	///     Это канал-источник расписания, репостить сам в себя нельзя
	/// </summary>
	SourceChannel = 2,

	/// <summary>
	///     Нет прав на отправку сообщений
	/// </summary>
	NoWritePermission = 3,

	/// <summary>
	///     Нет прав на отправку медиа
	/// </summary>
	NoMediaPermission = 4,

	/// <summary>
	///     Канал не удалось найти или вступить в него
	/// </summary>
	NotResolved = 5,

	/// <summary>
	///     На этом канале Telegram ограничил аккаунт
	/// </summary>
	RateLimited = 6,

	/// <summary>
	///     Канал не обработан: задание остановлено и не будет продолжено
	/// </summary>
	NotProcessed = 7,

	/// <summary>
	///     Канал в очереди на фоновую обработку
	/// </summary>
	Pending = 8
}

namespace Shared.Enums;

/// <summary>
///     Детальная причина, по которой репост завершился именно так
/// </summary>
public enum RepostLogReason
{
	/// <summary>
	///     Причина не требуется — репост выполнен
	/// </summary>
	None = 0,

	/// <summary>
	///     Пропущено настройкой «репостить каждое N-е сообщение»
	/// </summary>
	EveryNth = 1,

	/// <summary>
	///     Пропущено случайным образом по вероятности пропуска
	/// </summary>
	SkipProbability = 2,

	/// <summary>
	///     Достигнут дневной лимит репостов в этот канал
	/// </summary>
	DailyLimit = 3,

	/// <summary>
	///     Сообщение ещё не опубликовано в канале-источнике, репостить нечего
	/// </summary>
	MessageNotPublished = 4,

	/// <summary>
	///     Не удалось получить список диалогов Telegram-сессии
	/// </summary>
	DialogsUnavailable = 5,

	/// <summary>
	///     Канал-источник не найден Telegram-сессией
	/// </summary>
	SourceChannelNotResolved = 6,

	/// <summary>
	///     Целевой канал отсутствует в диалогах сессии — аккаунт в нём не состоит
	/// </summary>
	DestinationNotAvailable = 7,

	/// <summary>
	///     Аккаунт заблокирован в целевом канале, направление отключено
	/// </summary>
	Banned = 8,

	/// <summary>
	///     Telegram вернул ошибку при пересылке сообщения
	/// </summary>
	ForwardFailed = 9
}

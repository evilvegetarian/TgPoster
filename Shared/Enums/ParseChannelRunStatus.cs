namespace Shared.Enums;

/// <summary>
/// Чем закончился один проход источника парсинга
/// </summary>
public enum ParseChannelRunStatus
{
	/// <summary>
	/// История получена, новые посты отправлены на обработку
	/// </summary>
	Success,

	/// <summary>
	/// Временный сбой Telegram (FloodWait, таймаут): позиция не сдвинута, источник попробуют в следующий раз
	/// </summary>
	TemporaryFailure,

	/// <summary>
	/// Канал не найден, аккаунт в нём забанен или у него нет доступа
	/// </summary>
	SourceUnavailable,

	/// <summary>
	/// Парсинг упал с ошибкой
	/// </summary>
	Failed
}

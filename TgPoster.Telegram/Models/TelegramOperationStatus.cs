namespace TgPoster.Telegram.Models;

/// <summary>
///     Унифицированный статус результата операции с Telegram API.
/// </summary>
public enum TelegramOperationStatus
{
	Success,
	UsernameNotFound,
	ChannelBanned,
	FloodWait,
	AccessDenied,
	Timeout,
	SessionNotFound,

	/// <summary>
	///     Медленный режим (slow mode): нужно подождать перед следующей отправкой
	/// </summary>
	SlowMode,

	/// <summary>
	///     Аккаунт временно ограничен за спам (PEER_FLOOD)
	/// </summary>
	SpamRestricted,

	/// <summary>
	///     В канале-источнике включена защита контента — пересылка запрещена
	/// </summary>
	ForwardsRestricted,

	/// <summary>
	///     Тема форума в целевом чате закрыта или удалена — отправка и пересылка в неё запрещены
	/// </summary>
	TopicClosed,
	UnknownError
}
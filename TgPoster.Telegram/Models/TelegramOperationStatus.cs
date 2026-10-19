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

	/// <summary>
	/// Аккаунт достиг лимита каналов и групп (CHANNELS_TOO_MUCH): ни в один новый канал не вступит,
	/// пока не выйдет из ненужных
	/// </summary>
	ChannelsLimitReached,

	/// <summary>
	/// В чате нельзя писать рядовым участникам (CHAT_WRITE_FORBIDDEN, платные сообщения): общая настройка чата,
	/// а не бан аккаунта
	/// </summary>
	NoWritePermission,

	/// <summary>
	/// В чате нельзя отправлять медиа (CHAT_SEND_*_FORBIDDEN): общая настройка чата, а не бан аккаунта
	/// </summary>
	NoMediaPermission,
	UnknownError
}
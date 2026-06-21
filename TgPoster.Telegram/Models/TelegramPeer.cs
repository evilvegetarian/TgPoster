namespace TgPoster.Telegram.Models;

/// <summary>
///     Тип Telegram-пира (адресата API-вызова)
/// </summary>
public enum TelegramPeerType
{
	/// <summary>Канал или супергруппа (доступ по id + access_hash)</summary>
	Channel,

	/// <summary>Обычная группа (доступ только по id)</summary>
	Chat,

	/// <summary>Пользователь (доступ по id + access_hash)</summary>
	User
}

/// <summary>
///     Лёгкий handle, идентифицирующий Telegram-чат/канал/пользователя для API-вызовов.
///     Не содержит ссылок на внутренние типы WTelegram
/// </summary>
/// <param name="Id">Идентификатор сущности</param>
/// <param name="AccessHash">Access hash (0 для обычных групп)</param>
/// <param name="Type">Тип пира</param>
public sealed record TelegramPeer(long Id, long AccessHash, TelegramPeerType Type)
{
	/// <summary>Создаёт peer для канала или супергруппы</summary>
	public static TelegramPeer Channel(long id, long accessHash) => new(id, accessHash, TelegramPeerType.Channel);

	/// <summary>Создаёт peer для обычной группы</summary>
	public static TelegramPeer Chat(long id) => new(id, 0, TelegramPeerType.Chat);

	/// <summary>Создаёт peer для пользователя</summary>
	public static TelegramPeer User(long id, long accessHash) => new(id, accessHash, TelegramPeerType.User);
}
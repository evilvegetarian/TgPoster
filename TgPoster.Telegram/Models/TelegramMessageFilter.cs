namespace TgPoster.Telegram.Models;

/// <summary>
///     Серверный фильтр для <c>Messages_Search</c>: ограничивает выборку только сообщениями нужного типа.
///     Используется в Discover, чтобы запрашивать только посты со ссылками
/// </summary>
public enum TelegramMessageFilter
{
	/// <summary>Только сообщения, содержащие URL</summary>
	Url,

	/// <summary>Только фотографии</summary>
	Photo,

	/// <summary>Только видео</summary>
	Video,

	/// <summary>Только документы</summary>
	Document
}
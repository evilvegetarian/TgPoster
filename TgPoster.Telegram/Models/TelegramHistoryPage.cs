namespace TgPoster.Telegram.Models;

/// <summary>
///     Страница истории сообщений Telegram-канала/чата с метаданными
/// </summary>
public sealed class TelegramHistoryPage
{
	/// <summary>Сообщения на странице (только реальные Message, без сервисных)</summary>
	public required IReadOnlyList<TelegramMessage> Messages { get; init; }

	/// <summary>
	///     Связанные чаты, упомянутые в сообщениях (если сервер вернул их в ответе).
	///     Используется Discover для извлечения метаданных каналов без дополнительных API-вызовов
	/// </summary>
	public IReadOnlyList<TelegramChatInfo> Chats { get; init; } = [];
}

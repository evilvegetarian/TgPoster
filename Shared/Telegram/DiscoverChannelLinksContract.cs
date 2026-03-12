namespace Shared.Telegram;

/// <summary>
///     Контракт MassTransit для обнаружения TG-ссылок в канале.
/// </summary>
public sealed class DiscoverChannelLinksContract
{

	/// <summary>
	///     Username канала для парсинга.
	/// </summary>
	public required string ChannelUsername { get; init; }

	/// <summary>
	///     ID Telegram-сессии для авторизации.
	/// </summary>
	public required Guid TelegramSessionId { get; init; }

	/// <summary>
	///     Глубина рекурсии (0 = исходный канал, 1 = найденный канал).
	/// </summary>
	public int Depth { get; init; }
}

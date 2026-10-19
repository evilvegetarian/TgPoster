namespace TgPoster.Telegram.Models;

/// <summary>
/// Чаты аккаунта из одного запроса getAllChats: где он участник и где его забанили или исключили
/// </summary>
public sealed class TelegramJoinedChats
{
	/// <summary>
	/// Чаты, где аккаунт участнк, по raw-id
	/// </summary>
	public required IReadOnlyDictionary<long, TelegramChatInfo> Chats { get; init; }

	/// <summary>
	/// Чаты, которые Telegram отдал недоступными аккаунту (ChannelForbidden/ChatForbidden): бан или исключение
	/// </summary>
	public required IReadOnlySet<long> ForbiddenChatIds { get; init; }
}

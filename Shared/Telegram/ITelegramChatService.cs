using WTelegram;

namespace Shared.Telegram;

/// <summary>
///     Интерфейс для работы с чатами и каналами Telegram.
/// </summary>
public interface ITelegramChatService
{
	Task<TelegramChatInfo> GetChatInfoAsync(Client client, string input, bool autoJoin = true);
	void EnsureCanSendMessages(TelegramChatInfo chatInfo);
	Task<TelegramChannelInfoResult> GetFullChannelInfoAsync(Client client, TelegramChatInfo chatInfo);
	Task<TelegramChannelRefreshResult> RefreshChannelInfoAsync(Client client, long chatId);
	Task<(long LinkedChatId, long? DiscussionAccessHash)> GetLinkedDiscussionGroupAsync(
		Client client, TelegramChatInfo chatInfo, CancellationToken ct = default);

	/// <summary>
	///     Получает общее количество сообщений в канале.
	/// </summary>
	Task<int?> GetChannelMessagesCountAsync(Client client, string channelUsername);
}

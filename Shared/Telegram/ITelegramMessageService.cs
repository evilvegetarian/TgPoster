using TL;
using WTelegram;

namespace Shared.Telegram;

/// <summary>
///     Унифицированный сервис для низкоуровневой работы с Telegram через WTelegram.Client.
///     Инкапсулирует маппинг RpcException в <see cref="TelegramOperationStatus" />, автоматический
///     FLOOD_WAIT-retry и обновление FILE_REFERENCE при скачивании медиа.
/// </summary>
public interface ITelegramMessageService
{
	/// <summary>
	///     Разрешает канал/чат по username (Contacts_ResolveUsername).
	/// </summary>
	Task<TelegramOperationResult<Channel>> ResolveChannelAsync(
		Client client,
		string username,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Получает все диалоги клиента (Messages_GetAllDialogs).
	/// </summary>
	Task<TelegramOperationResult<Messages_Dialogs>> GetAllDialogsAsync(
		Client client,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	);

	/// <summary>
	///     Получает историю сообщений канала/чата (Messages_GetHistory).
	/// </summary>
	Task<TelegramOperationResult<Messages_MessagesBase>> GetHistoryAsync(
		Client client,
		InputPeer peer,
		int limit,
		int offsetId = 0,
		DateTime? offsetDate = null,
		int minId = 0,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	);

	/// <summary>
	///     Получает одно сообщение из канала по ID (Channels_GetMessages).
	/// </summary>
	Task<TelegramOperationResult<Message?>> GetChannelMessageAsync(
		Client client,
		InputChannel channel,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Проверяет инвайт-ссылку (Messages_CheckChatInvite).
	/// </summary>
	Task<TelegramOperationResult<ChatInviteBase>> CheckChatInviteAsync(
		Client client,
		string hash,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Пересылает одно сообщение (Messages_ForwardMessages). Возвращает ID нового сообщения в destination.
	/// </summary>
	Task<TelegramOperationResult<int?>> ForwardMessageAsync(
		Client client,
		InputPeer from,
		InputPeer to,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Отправляет текстовое сообщение (Messages_SendMessage). Возвращает ID созданного сообщения.
	/// </summary>
	Task<TelegramOperationResult<int?>> SendMessageAsync(
		Client client,
		InputPeer peer,
		string text,
		int? replyToMsgId = null,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Получает ID комментария для поста в канале с привязанной группой (Messages_GetDiscussionMessage).
	/// </summary>
	Task<TelegramOperationResult<int?>> GetDiscussionMessageIdAsync(
		Client client,
		InputPeer channelPeer,
		int postId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Экспортирует ссылку на сообщение канала (Channels_ExportMessageLink).
	/// </summary>
	Task<TelegramOperationResult<string>> ExportMessageLinkAsync(
		Client client,
		InputChannel channel,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Скачивает фотографию с автоматической обработкой FILE_REFERENCE_EXPIRED (refresh из сообщения).
	/// </summary>
	Task<TelegramOperationResult> DownloadPhotoAsync(
		Client client,
		InputChannel channel,
		int messageId,
		Photo photo,
		Stream target,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	);

	/// <summary>
	///     Получает точное количество участников канала (Channels_GetFullChannel).
	/// </summary>
	Task<TelegramOperationResult<int?>> GetFullChannelAsync(
		Client client,
		InputChannel channel,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Скачивает документ/видео с автоматической обработкой FILE_REFERENCE_EXPIRED (refresh из сообщения).
	/// </summary>
	Task<TelegramOperationResult> DownloadDocumentAsync(
		Client client,
		InputChannel channel,
		int messageId,
		Document document,
		Stream target,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	);
}
using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Abstractions;

/// <summary>
///     Унифицированный сервис для работы с Telegram через WTelegram-клиент.
///     Инкапсулирует получение клиента по идентификатору сессии, маппинг RpcException в
///     <see cref="TelegramOperationStatus"/>, автоматический FLOOD_WAIT-retry и обновление
///     FILE_REFERENCE при скачивании медиа
/// </summary>
public interface ITelegramMessageService
{
	/// <summary>
	///     Разрешает канал/чат по username
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="username">Username канала (без @)</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT (true) или вернуть результат (false)</param>
	/// <returns>Информация о найденном чате/канале</returns>
	Task<TelegramOperationResult<TelegramChatInfo>> ResolveChannelAsync(
		Guid sessionId,
		string username,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Получает все диалоги клиента
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>Список диалогов как информация о чатах</returns>
	Task<TelegramOperationResult<IReadOnlyList<TelegramChatInfo>>> GetAllDialogsAsync(
		Guid sessionId,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	);

	/// <summary>
	///     Получает историю сообщений канала/чата
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="peer">Peer-handle канала/чата</param>
	/// <param name="limit">Максимум сообщений</param>
	/// <param name="offsetId">ID сообщения-смещения (0 — с начала)</param>
	/// <param name="offsetDate">Дата-смещение</param>
	/// <param name="minId">Минимальный ID (0 — без ограничения)</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>Страница сообщений</returns>
	Task<TelegramOperationResult<TelegramHistoryPage>> GetHistoryAsync(
		Guid sessionId,
		TelegramPeer peer,
		int limit,
		int offsetId = 0,
		DateTime? offsetDate = null,
		int minId = 0,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	);

	/// <summary>
	///     Ищет сообщения в peer'е с серверным фильтром.
	///     Используется в Discover для выборки только сообщений со ссылками
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="peer">Peer-handle канала/чата</param>
	/// <param name="filter">Серверный фильтр</param>
	/// <param name="limit">Максимум сообщений</param>
	/// <param name="offsetId">ID-смещение</param>
	/// <param name="minId">Минимальный ID</param>
	/// <param name="maxId">Максимальный ID</param>
	/// <param name="query">Текстовый запрос</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>Страница сообщений, удовлетворяющих фильтру</returns>
	Task<TelegramOperationResult<TelegramHistoryPage>> SearchMessagesAsync(
		Guid sessionId,
		TelegramPeer peer,
		TelegramMessageFilter filter,
		int limit,
		int offsetId = 0,
		int minId = 0,
		int maxId = 0,
		string query = "",
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	);

	/// <summary>
	///     Получает одно сообщение из канала по ID
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="channel">Peer канала</param>
	/// <param name="messageId">ID сообщения</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>Сообщение или null если не найдено</returns>
	Task<TelegramOperationResult<TelegramMessage?>> GetChannelMessageAsync(
		Guid sessionId,
		TelegramPeer channel,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Пересылает одно сообщение
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="from">Откуда пересылаем</param>
	/// <param name="to">Куда пересылаем</param>
	/// <param name="messageId">ID сообщения-источника</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>ID нового сообщения в destination</returns>
	Task<TelegramOperationResult<int?>> ForwardMessageAsync(
		Guid sessionId,
		TelegramPeer from,
		TelegramPeer to,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Отправляет текстовое сообщение
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="peer">Peer получателя</param>
	/// <param name="text">Текст сообщения</param>
	/// <param name="replyToMsgId">ID сообщения, на которое отвечаем (опционально)</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>ID созданного сообщения</returns>
	Task<TelegramOperationResult<int?>> SendMessageAsync(
		Guid sessionId,
		TelegramPeer peer,
		string text,
		int? replyToMsgId = null,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Получает ID комментария к посту в канале с привязанной группой обсуждения
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="channelPeer">Peer канала</param>
	/// <param name="postId">ID поста</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>ID комментария или null</returns>
	Task<TelegramOperationResult<int?>> GetDiscussionMessageIdAsync(
		Guid sessionId,
		TelegramPeer channelPeer,
		int postId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Экспортирует ссылку на сообщение канала
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="channel">Peer канала</param>
	/// <param name="messageId">ID сообщения</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>Полная ссылка на сообщение</returns>
	Task<TelegramOperationResult<string>> ExportMessageLinkAsync(
		Guid sessionId,
		TelegramPeer channel,
		int messageId,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Получает точное количество участников канала
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="channel">Peer канала</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	/// <returns>Количество участников или null</returns>
	Task<TelegramOperationResult<int?>> GetFullChannelAsync(
		Guid sessionId,
		TelegramPeer channel,
		CancellationToken ct = default,
		bool waitOnFloodWait = false
	);

	/// <summary>
	///     Скачивает медиа-вложение сообщения (фото или документ/видео) с автоматической
	///     обработкой FILE_REFERENCE_EXPIRED (refresh из сообщения)
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="channel">Peer канала, в котором сообщение</param>
	/// <param name="messageId">ID сообщения с медиа</param>
	/// <param name="media">Медиа-объект из <see cref="TelegramMessage.Media"/></param>
	/// <param name="target">Поток-приёмник</param>
	/// <param name="ct">Токен отмены</param>
	/// <param name="waitOnFloodWait">Ждать при FLOOD_WAIT</param>
	Task<TelegramOperationResult> DownloadMediaAsync(
		Guid sessionId,
		TelegramPeer channel,
		int messageId,
		TelegramMessageMedia media,
		Stream target,
		CancellationToken ct = default,
		bool waitOnFloodWait = true
	);
}

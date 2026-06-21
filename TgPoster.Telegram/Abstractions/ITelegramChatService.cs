using TgPoster.Telegram.Models;

namespace TgPoster.Telegram.Abstractions;

/// <summary>
///     Высокоуровневый сервис для работы с Telegram-чатами/каналами: получение информации,
///     парсинг ссылок-приглашений, проверка прав, расчёт количества участников
/// </summary>
public interface ITelegramChatService
{
	/// <summary>
	///     Получает информацию о чате/канале по входной строке (username, ссылка-приглашение,
	///     числовой ID или t.me/c/-ссылка)
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="input">Входная строка</param>
	/// <param name="autoJoin">Автоматически вступать в приватные чаты по invite-ссылке</param>
	/// <returns>Информация о чате/канале</returns>
	Task<TelegramChatInfo> GetChatInfoAsync(Guid sessionId, string input, bool autoJoin = true);

	/// <summary>
	///     Проверяет, что в чате можно отправлять сообщения, иначе бросает исключение
	/// </summary>
	/// <param name="chatInfo">Информация о чате</param>
	void EnsureCanSendMessages(TelegramChatInfo chatInfo);

	/// <summary>
	///     Получает расширенную информацию о канале/чате: количество подписчиков, аватарку
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="chatInfo">Базовая информация о чате</param>
	/// <returns>Расширенная информация</returns>
	Task<TelegramChannelInfoResult> GetFullChannelInfoAsync(Guid sessionId, TelegramChatInfo chatInfo);

	/// <summary>
	///     Обновляет информацию о канале/чате. При бане или отсутствии возвращает соответствующий статус
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="chatId">Числовой ID чата</param>
	/// <returns>Обновлённая информация со статусом</returns>
	Task<TelegramChannelRefreshResult> RefreshChannelInfoAsync(Guid sessionId, long chatId);

	/// <summary>
	///     Получает информацию о привязанной группе обсуждений канала
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="chatInfo">Информация о канале</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Tuple: ID привязанной группы (0 если нет) и её access_hash</returns>
	Task<(long LinkedChatId, long? DiscussionAccessHash)> GetLinkedDiscussionGroupAsync(
		Guid sessionId,
		TelegramChatInfo chatInfo,
		CancellationToken ct = default
	);

	/// <summary>
	///     Получает общее количество сообщений в канале
	/// </summary>
	/// <param name="sessionId">Идентификатор Telegram-сессии</param>
	/// <param name="channelUsername">Username канала</param>
	/// <returns>Количество сообщений или null если канал не найден</returns>
	Task<int?> GetChannelMessagesCountAsync(Guid sessionId, string channelUsername);
}
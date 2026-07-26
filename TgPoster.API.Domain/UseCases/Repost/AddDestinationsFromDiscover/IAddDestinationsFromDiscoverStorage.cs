using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

/// <summary>
///     Настройки репоста, нужные для массового добавления каналов
/// </summary>
/// <param name="TelegramSessionId">Id телеграм сессии для выполнения репостов</param>
/// <param name="SourceChannelId">Id канала-источника из расписания</param>
/// <param name="DefaultDelayMinSeconds">Общая минимальная задержка перед репостом (секунды)</param>
/// <param name="DefaultDelayMaxSeconds">Общая максимальная задержка перед репостом (секунды)</param>
/// <param name="DefaultRepostEveryNth">Общая настройка "репостить каждое N-е сообщение"</param>
/// <param name="DefaultSkipProbability">Общая вероятность пропуска репоста (0-100%)</param>
/// <param name="DefaultMaxRepostsPerDay">Общий лимит репостов в день (null = без лимита)</param>
public sealed record RepostSettingsDefaults(
	Guid TelegramSessionId,
	long SourceChannelId,
	int DefaultDelayMinSeconds,
	int DefaultDelayMaxSeconds,
	int DefaultRepostEveryNth,
	int DefaultSkipProbability,
	int? DefaultMaxRepostsPerDay);

/// <summary>
///     Канал из Discover, выбранный для добавления в репост
/// </summary>
/// <param name="Id">Id записи в Discover</param>
/// <param name="TelegramId">Числовой Id канала в Telegram (null, если ещё не резолвился)</param>
/// <param name="Username">Username без @ (null для приватных каналов)</param>
/// <param name="Title">Название канала</param>
/// <param name="InviteHash">Хеш инвайт-ссылки для приватных каналов</param>
/// <param name="ParticipantsCount">Количество подписчиков по данным Discover</param>
public sealed record DiscoverCandidate(
	Guid Id,
	long? TelegramId,
	string? Username,
	string? Title,
	string? InviteHash,
	int? ParticipantsCount);

public interface IAddDestinationsFromDiscoverStorage
{
	/// <summary>
	///     Получить настройки репоста пользователя вместе с каналом-источником
	/// </summary>
	/// <param name="repostSettingsId">Id настроек репоста</param>
	/// <param name="userId">Id пользователя-владельца</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Настройки или null, если их нет или они принадлежат другому пользователю</returns>
	Task<RepostSettingsDefaults?> GetSettingsDefaultsAsync(Guid repostSettingsId, Guid userId, CancellationToken ct);

	/// <summary>
	///     Получить выбранные каналы из Discover
	/// </summary>
	/// <param name="discoveredChannelIds">Id записей в Discover</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Найденные каналы (забаненные исключены глобальным фильтром)</returns>
	Task<List<DiscoverCandidate>> GetCandidatesAsync(IReadOnlyList<Guid> discoveredChannelIds, CancellationToken ct);

	/// <summary>
	///     Получить Telegram-идентификаторы уже добавленных целевых каналов
	/// </summary>
	/// <param name="repostSettingsId">Id настроек репоста</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Список ChatId существующих направлений</returns>
	Task<List<long>> GetExistingChatIdsAsync(Guid repostSettingsId, CancellationToken ct);

	/// <summary>
	///     Обновить в Discover данные канала, полученные из Telegram
	/// </summary>
	/// <param name="discoveredChannelId">Id записи в Discover</param>
	/// <param name="telegramId">Числовой Id канала в Telegram</param>
	/// <param name="title">Название канала</param>
	/// <param name="username">Username без @</param>
	/// <param name="chatType">Тип чата</param>
	/// <param name="canSendMessages">Можно ли отправлять сообщения</param>
	/// <param name="canSendMedia">Можно ли отправлять медиа</param>
	/// <param name="ct">Токен отмены</param>
	Task UpdateDiscoveredChannelAsync(
		Guid discoveredChannelId,
		long telegramId,
		string? title,
		string? username,
		ChatType chatType,
		bool canSendMessages,
		bool canSendMedia,
		CancellationToken ct
	);

	/// <summary>
	///     Создать целевой канал репоста
	/// </summary>
	/// <param name="repostSettingsId">Id настроек репоста</param>
	/// <param name="chatId">Числовой Id канала в Telegram</param>
	/// <param name="title">Название канала</param>
	/// <param name="username">Username без @</param>
	/// <param name="memberCount">Количество подписчиков</param>
	/// <param name="chatType">Тип чата</param>
	/// <param name="discoveredChannelId">Id связанной записи в Discover</param>
	/// <param name="delayMinSeconds">Минимальная задержка перед репостом (секунды)</param>
	/// <param name="delayMaxSeconds">Максимальная задержка перед репостом (секунды)</param>
	/// <param name="repostEveryNth">Репостить каждое N-е сообщение</param>
	/// <param name="skipProbability">Вероятность пропуска репоста (0-100%)</param>
	/// <param name="maxRepostsPerDay">Лимит репостов в день (null = без лимита)</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Id созданного целевого канала</returns>
	Task<Guid> AddDestinationAsync(
		Guid repostSettingsId,
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		Guid discoveredChannelId,
		int delayMinSeconds,
		int delayMaxSeconds,
		int repostEveryNth,
		int skipProbability,
		int? maxRepostsPerDay,
		CancellationToken ct
	);
}

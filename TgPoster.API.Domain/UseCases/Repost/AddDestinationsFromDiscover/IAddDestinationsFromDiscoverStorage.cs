using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

/// <summary>
///     Настройки репоста, нужные для постановки каналов в очередь
/// </summary>
/// <param name="TelegramSessionId">Id телеграм сессии для выполнения репостов</param>
/// <param name="SourceChannelId">Id канала-источника из расписания</param>
/// <param name="SessionFloodWaitUntil">До какого момента Telegram ограничил сессию (null — ограничений нет)</param>
public sealed record RepostSettingsDefaults(
	Guid TelegramSessionId,
	long SourceChannelId,
	DateTimeOffset? SessionFloodWaitUntil);

/// <summary>
///     Канал из Discover, выбранный для добавления в репост
/// </summary>
/// <param name="Id">Id записи в Discover</param>
/// <param name="TelegramId">Числовой Id канала в Telegram (null, если ещё не резолвился)</param>
/// <param name="Username">Username без @ (null для приватных каналов)</param>
/// <param name="Title">Название канала</param>
/// <param name="InviteHash">Хеш инвайт-ссылки для приватных каналов</param>
/// <param name="CanSendMessages">Известное из прошлых проверок право на отправку сообщений (null — не проверялось)</param>
/// <param name="CanSendMedia">Известное из прошлых проверок право на отправку медиа (null — не проверялось)</param>
public sealed record DiscoverCandidate(
	Guid Id,
	long? TelegramId,
	string? Username,
	string? Title,
	string? InviteHash,
	bool? CanSendMessages,
	bool? CanSendMedia);

/// <summary>
///     Канал, попадающий в задание на массовое добавление
/// </summary>
/// <param name="DiscoveredChannelId">Id записи в Discover</param>
/// <param name="Title">Название канала для отображения в интерфейсе</param>
/// <param name="Outcome">Стартовый итог: Pending — ждёт фоновой обработки, остальное — решено сразу</param>
/// <param name="Error">Причина отказа, если канал отбракован сразу</param>
public sealed record ImportJobItemDto(
	Guid DiscoveredChannelId,
	string Title,
	AddDestinationOutcome Outcome,
	string? Error);

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
	///     Получить каналы Discover, подходящие под фильтр. Заведомо бесполезные кандидаты
	///     отсекаются здесь же, чтобы лимит задания не расходовался впустую
	/// </summary>
	/// <param name="filter">Фильтр и сортировка Discover</param>
	/// <param name="excludedChatIds">Telegram-идентификаторы, которые брать не нужно: уже добавленные и канал-источник</param>
	/// <param name="limit">Максимальное количество каналов в задании</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Каналы в порядке сортировки фильтра</returns>
	Task<List<DiscoverCandidate>> GetCandidatesByFilterAsync(
		DiscoverImportFilter filter,
		IReadOnlyCollection<long> excludedChatIds,
		int limit,
		CancellationToken ct
	);

	/// <summary>
	///     Получить Telegram-идентификаторы уже добавленных целевых каналов
	/// </summary>
	/// <param name="repostSettingsId">Id настроек репоста</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Список ChatId существующих направлений</returns>
	Task<List<long>> GetExistingChatIdsAsync(Guid repostSettingsId, CancellationToken ct);

	/// <summary>
	///     Найти незавершённое задание на добавление каналов для этих настроек репоста
	/// </summary>
	/// <param name="repostSettingsId">Id настроек репоста</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Id активного задания или null, если все задания завершены</returns>
	Task<Guid?> GetActiveJobIdAsync(Guid repostSettingsId, CancellationToken ct);

	/// <summary>
	///     Создать задание на массовое добавление каналов вместе со всеми его каналами
	/// </summary>
	/// <param name="repostSettingsId">Id настроек репоста</param>
	/// <param name="autoJoin">Вступать ли автоматически в приватные каналы</param>
	/// <param name="items">Каналы задания с уже определённым стартовым итогом</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Id созданного задания</returns>
	Task<Guid> CreateImportJobAsync(
		Guid repostSettingsId,
		bool autoJoin,
		IReadOnlyList<ImportJobItemDto> items,
		CancellationToken ct
	);
}

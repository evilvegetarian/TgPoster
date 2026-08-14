using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;

/// <summary>
///     Задание на массовое добавление целевых каналов вместе с настройками репоста
/// </summary>
/// <param name="JobId">Id задания</param>
/// <param name="RepostSettingsId">Id настроек репоста</param>
/// <param name="TelegramSessionId">Id телеграм сессии, через которую резолвим и вступаем</param>
/// <param name="SourceChannelId">Id канала-источника из расписания</param>
/// <param name="SessionFloodWaitUntil">До какого момента Telegram ограничил сессию (null — ограничений нет)</param>
/// <param name="AutoJoin">Вступать ли автоматически в приватные каналы</param>
/// <param name="Status">Текущий статус задания</param>
/// <param name="DefaultDelayMinSeconds">Общая минимальная задержка перед репостом (секунды)</param>
/// <param name="DefaultDelayMaxSeconds">Общая максимальная задержка перед репостом (секунды)</param>
/// <param name="DefaultRepostEveryNth">Общая настройка "репостить каждое N-е сообщение"</param>
/// <param name="DefaultSkipProbability">Общая вероятность пропуска репоста (0-100%)</param>
/// <param name="DefaultMaxRepostsPerDay">Общий лимит репостов в день (null = без лимита)</param>
public sealed record ImportJobData(
	Guid JobId,
	Guid RepostSettingsId,
	Guid TelegramSessionId,
	long SourceChannelId,
	DateTimeOffset? SessionFloodWaitUntil,
	bool AutoJoin,
	RepostImportStatus Status,
	int DefaultDelayMinSeconds,
	int DefaultDelayMaxSeconds,
	int DefaultRepostEveryNth,
	int DefaultSkipProbability,
	int? DefaultMaxRepostsPerDay);

/// <summary>
///     Канал задания, ожидающий обработки
/// </summary>
/// <param name="ItemId">Id строки задания</param>
/// <param name="DiscoveredChannelId">Id записи в Discover</param>
/// <param name="TelegramId">Числовой Id канала в Telegram (null, если ещё не резолвился)</param>
/// <param name="Username">Username без @ (null для приватных каналов)</param>
/// <param name="InviteHash">Хеш инвайт-ссылки для приватных каналов</param>
/// <param name="ParticipantsCount">Количество подписчиков по данным Discover</param>
/// <param name="CanSendMessages">Известное право на отправку сообщений (null — не проверялось)</param>
/// <param name="CanSendMedia">Известное право на отправку медиа (null — не проверялось)</param>
public sealed record ImportJobPendingItem(
	Guid ItemId,
	Guid DiscoveredChannelId,
	long? TelegramId,
	string? Username,
	string? InviteHash,
	int? ParticipantsCount,
	bool? CanSendMessages,
	bool? CanSendMedia);

public interface IImportRepostDestinationsStorage
{
	/// <summary>
	///     Получить задание вместе с настройками репоста и состоянием сессии
	/// </summary>
	/// <param name="jobId">Id задания</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Данные задания или null, если задания нет</returns>
	Task<ImportJobData?> GetJobAsync(Guid jobId, CancellationToken ct);

	/// <summary>
	///     Получить каналы задания, ожидающие обработки, вместе с актуальными данными из Discover
	/// </summary>
	/// <param name="jobId">Id задания</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Каналы в порядке постановки в очередь</returns>
	Task<List<ImportJobPendingItem>> GetPendingItemsAsync(Guid jobId, CancellationToken ct);

	/// <summary>
	///     Получить Telegram-идентификаторы уже добавленных целевых каналов
	/// </summary>
	/// <param name="repostSettingsId">Id настроек репоста</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Список ChatId существующих направлений</returns>
	Task<List<long>> GetExistingChatIdsAsync(Guid repostSettingsId, CancellationToken ct);

	/// <summary>
	///     Записать итог обработки канала
	/// </summary>
	/// <param name="itemId">Id строки задания</param>
	/// <param name="outcome">Итог обработки</param>
	/// <param name="repostDestinationId">Id созданного целевого канала (только при Outcome = Added)</param>
	/// <param name="error">Причина отказа</param>
	/// <param name="ct">Токен отмены</param>
	Task UpdateItemAsync(
		Guid itemId,
		AddDestinationOutcome outcome,
		Guid? repostDestinationId,
		string? error,
		CancellationToken ct
	);

	/// <summary>
	///     Обновить статус задания
	/// </summary>
	/// <param name="jobId">Id задания</param>
	/// <param name="status">Новый статус</param>
	/// <param name="error">Причина остановки (null — очистить)</param>
	/// <param name="ct">Токен отмены</param>
	Task SetJobStatusAsync(Guid jobId, RepostImportStatus status, string? error, CancellationToken ct);

	/// <summary>
	///     Запомнить, до какого момента Telegram ограничил сессию
	/// </summary>
	/// <param name="telegramSessionId">Id телеграм сессии</param>
	/// <param name="floodWaitUntil">Момент, после которого можно повторять запросы</param>
	/// <param name="ct">Токен отмены</param>
	Task SetSessionFloodWaitAsync(Guid telegramSessionId, DateTimeOffset floodWaitUntil, CancellationToken ct);

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

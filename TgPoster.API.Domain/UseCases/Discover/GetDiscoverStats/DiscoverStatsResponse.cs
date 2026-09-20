namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;

/// <summary>
///     Статистика по обнаруженным каналам: итоги, свежесть, разбивки и таймлайны
/// </summary>
public sealed record DiscoverStatsResponse
{
	/// <summary>Итоговые счётчики по всей базе</summary>
	public required DiscoverStatsTotals Totals { get; init; }

	/// <summary>Сколько спарсено и найдено за последние периоды</summary>
	public required DiscoverStatsFreshness Freshness { get; init; }

	/// <summary>Количество каналов по каждому статусу обработки (все статусы, включая нулевые)</summary>
	public required List<DiscoverStatusCount> ByStatus { get; init; }

	/// <summary>Количество по типу пира: channel / chat / неизвестно</summary>
	public required List<DiscoverNamedCount> ByPeerType { get; init; }

	/// <summary>Количество по тематике, по убыванию</summary>
	public required List<DiscoverNamedCount> ByCategory { get; init; }

	/// <summary>Количество по языку, по убыванию</summary>
	public required List<DiscoverNamedCount> ByLanguage { get; init; }

	/// <summary>Распределение по размеру аудитории</summary>
	public required List<DiscoverParticipantsBucketCount> ByParticipants { get; init; }

	/// <summary>Сколько каналов спарсено в каждый день окна (все дни, включая нулевые)</summary>
	public required List<DiscoverDailyCount> ParsedByDay { get; init; }

	/// <summary>Сколько новых каналов найдено в каждый день окна (все дни, включая нулевые)</summary>
	public required List<DiscoverDailyCount> FoundByDay { get; init; }

	/// <summary>Каналы, из которых найдено больше всего других каналов</summary>
	public required List<DiscoverSourceStat> TopSources { get; init; }
}

/// <summary>
///     Итоговые счётчики по обнаруженным каналам
/// </summary>
public sealed record DiscoverStatsTotals
{
	/// <summary>Всего каналов (без забаненных)</summary>
	public required int Total { get; init; }

	/// <summary>Каналов, у которых хотя бы раз парсились ссылки</summary>
	public required int Parsed { get; init; }

	/// <summary>Каналов, которые ещё ни разу не парсились</summary>
	public required int NotParsed { get; init; }

	/// <summary>Публичных каналов (есть username)</summary>
	public required int Public { get; init; }

	/// <summary>Приватных каналов (без username)</summary>
	public required int Private { get; init; }

	/// <summary>Каналов с проставленной тематикой</summary>
	public required int Classified { get; init; }

	/// <summary>Каналов с известным числом подписчиков</summary>
	public required int WithParticipants { get; init; }

	/// <summary>Суммарная аудитория всех каналов с известным числом подписчиков</summary>
	public required long TotalParticipants { get; init; }

	/// <summary>Забаненных каналов (в остальные счётчики не входят)</summary>
	public required int Banned { get; init; }
}

/// <summary>
///     Свежесть данных: сколько каналов спарсено и найдено за последние периоды
/// </summary>
public sealed record DiscoverStatsFreshness
{
	/// <summary>Спарсено за последние 24 часа</summary>
	public required int ParsedLast24Hours { get; init; }

	/// <summary>Спарсено за последние 7 дней</summary>
	public required int ParsedLast7Days { get; init; }

	/// <summary>Спарсено за последние 30 дней</summary>
	public required int ParsedLast30Days { get; init; }

	/// <summary>Найдено новых каналов за последние 24 часа</summary>
	public required int FoundLast24Hours { get; init; }

	/// <summary>Найдено новых каналов за последние 7 дней</summary>
	public required int FoundLast7Days { get; init; }

	/// <summary>Найдено новых каналов за последние 30 дней</summary>
	public required int FoundLast30Days { get; init; }

	/// <summary>Когда был найден самый первый канал</summary>
	public DateTimeOffset? FirstFoundAt { get; init; }

	/// <summary>Когда был найден последний новый канал</summary>
	public DateTimeOffset? LastFoundAt { get; init; }

	/// <summary>Когда последний раз парсился какой-либо канал</summary>
	public DateTimeOffset? LastParsedAt { get; init; }
}

/// <summary>
///     Количество каналов с одним статусом обработки
/// </summary>
public sealed record DiscoverStatusCount
{
	/// <summary>Статус обработки</summary>
	public required DiscoverChannelStatus Status { get; init; }

	/// <summary>Количество каналов</summary>
	public required int Count { get; init; }
}

/// <summary>
///     Количество каналов с одним значением признака (тематика, язык, тип)
/// </summary>
public sealed record DiscoverNamedCount
{
	/// <summary>Значение признака</summary>
	public required string Name { get; init; }

	/// <summary>Количество каналов</summary>
	public required int Count { get; init; }
}

/// <summary>
///     Корзина по размеру аудитории канала
/// </summary>
public enum DiscoverParticipantsBucket
{
	/// <summary>Число подписчиков неизвестно</summary>
	Unknown,

	/// <summary>Меньше 1 000</summary>
	UpTo1K,

	/// <summary>От 1 000 до 10 000</summary>
	From1KTo10K,

	/// <summary>От 10 000 до 100 000</summary>
	From10KTo100K,

	/// <summary>100 000 и больше</summary>
	Over100K
}

/// <summary>
///     Количество каналов в одной корзине по размеру аудитории
/// </summary>
public sealed record DiscoverParticipantsBucketCount
{
	/// <summary>Корзина</summary>
	public required DiscoverParticipantsBucket Bucket { get; init; }

	/// <summary>Количество каналов</summary>
	public required int Count { get; init; }
}

/// <summary>
///     Количество событий за один день
/// </summary>
public sealed record DiscoverDailyCount
{
	/// <summary>Дата (UTC)</summary>
	public required DateOnly Date { get; init; }

	/// <summary>Количество</summary>
	public required int Count { get; init; }
}

/// <summary>
///     Канал-источник и сколько других каналов из него найдено
/// </summary>
public sealed record DiscoverSourceStat
{
	/// <summary>ID канала-источника</summary>
	public required Guid Id { get; init; }

	/// <summary>Username канала-источника</summary>
	public string? Username { get; init; }

	/// <summary>Название канала-источника</summary>
	public string? Title { get; init; }

	/// <summary>URL аватарки</summary>
	public string? AvatarUrl { get; init; }

	/// <summary>Ссылка на канал</summary>
	public string? TgUrl { get; init; }

	/// <summary>Сколько каналов найдено из этого источника</summary>
	public required int FoundCount { get; init; }

	/// <summary>Когда источник парсился последний раз</summary>
	public DateTimeOffset? LastParsedAt { get; init; }

	/// <summary>Забанен ли источник (тогда он не показывается в общем списке)</summary>
	public required bool IsBanned { get; init; }
}

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;

/// <summary>
///     Итоговые счётчики по обнаруженным каналам, посчитанные одним запросом
/// </summary>
/// <param name="Total">Всего каналов</param>
/// <param name="Parsed">Каналов, которые хотя бы раз парсились</param>
/// <param name="Public">Каналов с username</param>
/// <param name="Classified">Каналов с тематикой</param>
/// <param name="WithParticipants">Каналов с известным числом подписчиков</param>
/// <param name="TotalParticipants">Суммарная аудитория</param>
/// <param name="ParsedLast24Hours">Спарсено за последние 24 часа</param>
/// <param name="ParsedLast7Days">Спарсено за последние 7 дней</param>
/// <param name="ParsedLast30Days">Спарсено за последние 30 дней</param>
/// <param name="FoundLast24Hours">Найдено за последние 24 часа</param>
/// <param name="FoundLast7Days">Найдено за последние 7 дней</param>
/// <param name="FoundLast30Days">Найдено за последние 30 дней</param>
/// <param name="FirstFoundAt">Когда найден первый канал</param>
/// <param name="LastFoundAt">Когда найден последний канал</param>
/// <param name="LastParsedAt">Когда последний раз парсился какой-либо канал</param>
public sealed record DiscoverStatsTotalsDto(
	int Total,
	int Parsed,
	int Public,
	int Classified,
	int WithParticipants,
	long TotalParticipants,
	int ParsedLast24Hours,
	int ParsedLast7Days,
	int ParsedLast30Days,
	int FoundLast24Hours,
	int FoundLast7Days,
	int FoundLast30Days,
	DateTimeOffset? FirstFoundAt,
	DateTimeOffset? LastFoundAt,
	DateTimeOffset? LastParsedAt)
{
	/// <summary>
	///     Пустые итоги: в базе ещё нет ни одного канала
	/// </summary>
	public static DiscoverStatsTotalsDto Empty { get; } = new(
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null, null);
}

public interface IGetDiscoverStatsStorage
{
	/// <summary>
	///     Посчитать итоговые счётчики и свежесть относительно переданного момента времени
	/// </summary>
	/// <param name="now"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<DiscoverStatsTotalsDto> GetTotalsAsync(DateTimeOffset now, CancellationToken ct);

	/// <summary>
	///     Посчитать забаненные каналы (они скрыты глобальным фильтром)
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<int> GetBannedCountAsync(CancellationToken ct);

	/// <summary>
	///     Количество каналов по статусам обработки (только ненулевые)
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverStatusCount>> GetStatusCountsAsync(CancellationToken ct);

	/// <summary>
	///     Количество каналов по типу пира; для null-типа возвращается пустая строка
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverNamedCount>> GetPeerTypeCountsAsync(CancellationToken ct);

	/// <summary>
	///     Количество каналов по тематике, по убыванию; каналы без тематики не учитываются
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverNamedCount>> GetCategoryCountsAsync(CancellationToken ct);

	/// <summary>
	///     Количество каналов по языку, по убыванию; каналы без языка не учитываются
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverNamedCount>> GetLanguageCountsAsync(CancellationToken ct);

	/// <summary>
	///     Распределение каналов по корзинам размера аудитории
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverParticipantsBucketCount>> GetParticipantsBucketCountsAsync(CancellationToken ct);

	/// <summary>
	///     Сколько каналов спарсено в каждый день, начиная с указанного момента (только ненулевые дни)
	/// </summary>
	/// <param name="since"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverDailyCount>> GetParsedByDayAsync(DateTimeOffset since, CancellationToken ct);

	/// <summary>
	///     Сколько каналов найдено в каждый день, начиная с указанного момента (только ненулевые дни)
	/// </summary>
	/// <param name="since"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverDailyCount>> GetFoundByDayAsync(DateTimeOffset since, CancellationToken ct);

	/// <summary>
	///     Каналы-источники с наибольшим числом найденых из них каналов
	/// </summary>
	/// <param name="limit"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverSourceStat>> GetTopSourcesAsync(int limit, CancellationToken ct);
}

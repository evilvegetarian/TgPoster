using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;

/// <summary>
///     Итоговые счётчики классификации, посчитанные одним запросом
/// </summary>
/// <param name="Total">Всего каналов</param>
/// <param name="Eligible">Каналов с username</param>
/// <param name="Classified">Каналов, классифицированных хотя бы раз</param>
/// <param name="Pending">Каналов с username, ещё ни разу не классифицированных</param>
/// <param name="WithCategory">Каналов с тематикой</param>
/// <param name="WithTags">Каналов хотя бы с одним тегом</param>
/// <param name="AverageConfidence">Средняя уверенность модели</param>
/// <param name="ClassifiedLast24Hours">Классифицировано за последние 24 часа</param>
/// <param name="ClassifiedLast7Days">Классифицировано за последние 7 дней</param>
/// <param name="ClassifiedLast30Days">Классифицировано за последние 30 дней</param>
/// <param name="LastClassifiedAt">Когда последний раз классифицировался какой-либо канал</param>
public sealed record ClassificationStatsTotalsDto(
	int Total,
	int Eligible,
	int Classified,
	int Pending,
	int WithCategory,
	int WithTags,
	double? AverageConfidence,
	int ClassifiedLast24Hours,
	int ClassifiedLast7Days,
	int ClassifiedLast30Days,
	DateTimeOffset? LastClassifiedAt)
{
	/// <summary>
	///     Пустые итоги: в базе ещё нет ни одного канала
	/// </summary>
	public static ClassificationStatsTotalsDto Empty { get; } = new(0, 0, 0, 0, 0, 0, null, 0, 0, 0, null);
}

public interface IGetClassificationStatsStorage
{
	/// <summary>
	///     Посчитать итоговые счётчики и свежесть относительно переданного момента времени
	/// </summary>
	/// <param name="now"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<ClassificationStatsTotalsDto> GetTotalsAsync(DateTimeOffset now, CancellationToken ct);

	/// <summary>
	///     Количество каналов и средняя уверенность по тематикам, по убыванию количества; каналы без тематики не учитываются
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ClassificationCategoryStat>> GetCategoryStatsAsync(CancellationToken ct);

	/// <summary>
	///     Количество каналов по языку, по убыванию; каналы без языка не учитываются
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverNamedCount>> GetLanguageCountsAsync(CancellationToken ct);

	/// <summary>
	///     Распределение классифицированных каналов по корзинам уверенности модели
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ClassificationConfidenceBucketCount>> GetConfidenceBucketCountsAsync(CancellationToken ct);

	/// <summary>
	///     Самые частые пары «тематика — подкатегория», по убыванию
	/// </summary>
	/// <param name="limit"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ClassificationSubcategoryStat>> GetTopSubcategoriesAsync(int limit, CancellationToken ct);

	/// <summary>
	///     Сколько разных пар «тематика — подкатегория» есть в базе
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<int> GetDistinctSubcategoryCountAsync(CancellationToken ct);

	/// <summary>
	///     Самые частые теги без учёта регистра, по убыванию
	/// </summary>
	/// <param name="limit"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverNamedCount>> GetTopTagsAsync(int limit, CancellationToken ct);

	/// <summary>
	///     Сколько разных тегов есть в базе без учёта регистра
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<int> GetDistinctTagCountAsync(CancellationToken ct);

	/// <summary>
	///     Сколько каналов классифицировано в каждый день, начиная с указанного момента (только ненулевые дни)
	/// </summary>
	/// <param name="since"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DiscoverDailyCount>> GetClassifiedByDayAsync(DateTimeOffset since, CancellationToken ct);
}

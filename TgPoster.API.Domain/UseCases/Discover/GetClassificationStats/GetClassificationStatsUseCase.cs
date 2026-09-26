using MediatR;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;

internal sealed class GetClassificationStatsUseCase(IGetClassificationStatsStorage storage)
	: IRequestHandler<GetClassificationStatsQuery, ClassificationStatsResponse>
{
	/// <summary>
	///     Максимальная глубина таймлайна: дальше по дням смотреть уже бессмысленно
	/// </summary>
	public const int MaxDays = 365;

	/// <summary>
	///     Сколько подкатегорий показывать в топе
	/// </summary>
	private const int TopSubcategoriesLimit = 15;

	/// <summary>
	///     Сколько тегов показывать в топе
	/// </summary>
	private const int TopTagsLimit = 40;

	public async Task<ClassificationStatsResponse> Handle(GetClassificationStatsQuery request, CancellationToken ct)
	{
		if (request.Days is < 1 or > MaxDays)
		{
			throw new InvalidDiscoverStatsPeriodException(request.Days, MaxDays);
		}

		var now = DateTimeOffset.UtcNow;
		var series = DailyCountSeries.EndingAt(request.Days, now);

		// Хранилище делит один DbContext, поэтому запросы идут строго последовательно
		var totals = await storage.GetTotalsAsync(now, ct);
		var byCategory = await storage.GetCategoryStatsAsync(ct);
		var byLanguage = await storage.GetLanguageCountsAsync(ct);
		var byConfidence = await storage.GetConfidenceBucketCountsAsync(ct);
		var topSubcategories = await storage.GetTopSubcategoriesAsync(TopSubcategoriesLimit, ct);
		var distinctSubcategories = await storage.GetDistinctSubcategoryCountAsync(ct);
		var topTags = await storage.GetTopTagsAsync(TopTagsLimit, ct);
		var distinctTags = await storage.GetDistinctTagCountAsync(ct);
		var classifiedByDay = await storage.GetClassifiedByDayAsync(series.Since, ct);

		return new ClassificationStatsResponse
		{
			Totals = new ClassificationStatsTotals
			{
				Total = totals.Total,
				Eligible = totals.Eligible,
				Skipped = totals.Total - totals.Eligible,
				Classified = totals.Classified,
				Pending = totals.Pending,
				WithCategory = totals.WithCategory,
				WithTags = totals.WithTags,
				AverageConfidence = totals.AverageConfidence,
				DistinctSubcategories = distinctSubcategories,
				DistinctTags = distinctTags
			},
			Freshness = new ClassificationStatsFreshness
			{
				ClassifiedLast24Hours = totals.ClassifiedLast24Hours,
				ClassifiedLast7Days = totals.ClassifiedLast7Days,
				ClassifiedLast30Days = totals.ClassifiedLast30Days,
				LastClassifiedAt = totals.LastClassifiedAt
			},
			ByCategory = byCategory,
			ByLanguage = byLanguage,
			ByConfidence = FillAllBuckets(byConfidence),
			TopSubcategories = topSubcategories,
			TopTags = topTags,
			ClassifiedByDay = series.Fill(classifiedByDay)
		};
	}

	/// <summary>
	///     Дополнить разбивку по уверенности нулями в порядке возрастания уверенности
	/// </summary>
	/// <param name="counts"></param>
	/// <returns></returns>
	private static List<ClassificationConfidenceBucketCount> FillAllBuckets(
		IReadOnlyCollection<ClassificationConfidenceBucketCount> counts
	) =>
		Enum.GetValues<ClassificationConfidenceBucket>()
			.Select(bucket => new ClassificationConfidenceBucketCount
			{
				Bucket = bucket,
				Count = counts.Where(x => x.Bucket == bucket).Sum(x => x.Count)
			})
			.ToList();
}

using MediatR;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;

internal sealed class GetDiscoverStatsUseCase(IGetDiscoverStatsStorage storage)
	: IRequestHandler<GetDiscoverStatsQuery, DiscoverStatsResponse>
{
	/// <summary>
	///     Максимальная глубина таймлайнов: дальше по дням смотреть уже бессмысленно
	/// </summary>
	public const int MaxDays = 365;

	/// <summary>
	///     Сколько каналов-источников показывать в топе
	/// </summary>
	private const int TopSourcesLimit = 10;

	/// <summary>
	///     Подпись для типа пира, который воркер не смог определить
	/// </summary>
	private const string UnknownPeerType = "unknown";

	public async Task<DiscoverStatsResponse> Handle(GetDiscoverStatsQuery request, CancellationToken ct)
	{
		if (request.Days is < 1 or > MaxDays)
		{
			throw new InvalidDiscoverStatsPeriodException(request.Days, MaxDays);
		}

		var now = DateTimeOffset.UtcNow;
		var today = DateOnly.FromDateTime(now.UtcDateTime);
		var firstDay = today.AddDays(-(request.Days - 1));
		var since = new DateTimeOffset(firstDay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

		// Хранилище делит один DbContext, поэтому запросы идут строго последовательно
		var totals = await storage.GetTotalsAsync(now, ct);
		var banned = await storage.GetBannedCountAsync(ct);
		var byStatus = await storage.GetStatusCountsAsync(ct);
		var byPeerType = await storage.GetPeerTypeCountsAsync(ct);
		var byCategory = await storage.GetCategoryCountsAsync(ct);
		var byLanguage = await storage.GetLanguageCountsAsync(ct);
		var byParticipants = await storage.GetParticipantsBucketCountsAsync(ct);
		var parsedByDay = await storage.GetParsedByDayAsync(since, ct);
		var foundByDay = await storage.GetFoundByDayAsync(since, ct);
		var topSources = await storage.GetTopSourcesAsync(TopSourcesLimit, ct);

		return new DiscoverStatsResponse
		{
			Totals = new DiscoverStatsTotals
			{
				Total = totals.Total,
				Parsed = totals.Parsed,
				NotParsed = totals.Total - totals.Parsed,
				Public = totals.Public,
				Private = totals.Total - totals.Public,
				Classified = totals.Classified,
				WithParticipants = totals.WithParticipants,
				TotalParticipants = totals.TotalParticipants,
				Banned = banned
			},
			Freshness = new DiscoverStatsFreshness
			{
				ParsedLast24Hours = totals.ParsedLast24Hours,
				ParsedLast7Days = totals.ParsedLast7Days,
				ParsedLast30Days = totals.ParsedLast30Days,
				FoundLast24Hours = totals.FoundLast24Hours,
				FoundLast7Days = totals.FoundLast7Days,
				FoundLast30Days = totals.FoundLast30Days,
				FirstFoundAt = totals.FirstFoundAt,
				LastFoundAt = totals.LastFoundAt,
				LastParsedAt = totals.LastParsedAt
			},
			ByStatus = FillAllStatuses(byStatus),
			ByPeerType = NormalizePeerTypes(byPeerType),
			ByCategory = byCategory,
			ByLanguage = byLanguage,
			ByParticipants = FillAllBuckets(byParticipants),
			ParsedByDay = FillAllDays(parsedByDay, firstDay, today),
			FoundByDay = FillAllDays(foundByDay, firstDay, today),
			TopSources = topSources
		};
	}

	/// <summary>
	///     Дополнить разбивку по статусам нулями, чтобы фронт всегда получал полный набор в фиксированном порядке
	/// </summary>
	/// <param name="counts"></param>
	/// <returns></returns>
	private static List<DiscoverStatusCount> FillAllStatuses(IReadOnlyCollection<DiscoverStatusCount> counts) =>
		Enum.GetValues<DiscoverChannelStatus>()
			.Select(status => new DiscoverStatusCount
			{
				Status = status,
				Count = counts.Where(x => x.Status == status).Sum(x => x.Count)
			})
			.ToList();

	/// <summary>
	///     Дополнить разбивку по корзинам аудитории нулями в порядке возрастания размера
	/// </summary>
	/// <param name="counts"></param>
	/// <returns></returns>
	private static List<DiscoverParticipantsBucketCount> FillAllBuckets(
		IReadOnlyCollection<DiscoverParticipantsBucketCount> counts
	) =>
		Enum.GetValues<DiscoverParticipantsBucket>()
			.Select(bucket => new DiscoverParticipantsBucketCount
			{
				Bucket = bucket,
				Count = counts.Where(x => x.Bucket == bucket).Sum(x => x.Count)
			})
			.ToList();

	/// <summary>
	///     Заменить пустой тип пира на читаемую подпись и отсортировать по убыванию
	/// </summary>
	/// <param name="counts"></param>
	/// <returns></returns>
	private static List<DiscoverNamedCount> NormalizePeerTypes(IEnumerable<DiscoverNamedCount> counts) =>
		counts
			.Select(x => string.IsNullOrEmpty(x.Name)
				? new DiscoverNamedCount { Name = UnknownPeerType, Count = x.Count }
				: x)
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Name)
			.ToList();

	/// <summary>
	///     Развернуть разреженный список по дням в плотный: каждый день окна присутствует, пустые дни — с нулём
	/// </summary>
	/// <param name="counts"></param>
	/// <param name="firstDay"></param>
	/// <param name="lastDay"></param>
	/// <returns></returns>
	private static List<DiscoverDailyCount> FillAllDays(
		IEnumerable<DiscoverDailyCount> counts,
		DateOnly firstDay,
		DateOnly lastDay
	)
	{
		var byDate = counts
			.GroupBy(x => x.Date)
			.ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

		var result = new List<DiscoverDailyCount>(lastDay.DayNumber - firstDay.DayNumber + 1);
		for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
		{
			result.Add(new DiscoverDailyCount
			{
				Date = day,
				Count = byDate.GetValueOrDefault(day)
			});
		}

		return result;
	}
}

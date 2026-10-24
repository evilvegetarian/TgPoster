using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.UseCases.Parse.GetParseChannelStats;

internal sealed class GetParseChannelStatsUseCase(
	IGetParseChannelStatsStorage storage,
	IIdentityProvider identity,
	TimeProvider timeProvider)
	: IRequestHandler<GetParseChannelStatsQuery, ParseChannelStatsResponse>
{
	/// <summary>
	/// Максимальная глубина таймлайнов: дальше по дням смотреть уже бессмысленно
	/// </summary>
	public const int MaxDays = 365;

	/// <summary>
	/// Сколько источников показывать в топе
	/// </summary>
	private const int TopSourcesLimit = 10;

	public async Task<ParseChannelStatsResponse> Handle(GetParseChannelStatsQuery request, CancellationToken ct)
	{
		if (request.Days is < 1 or > MaxDays)
		{
			throw new InvalidDiscoverStatsPeriodException(request.Days, MaxDays);
		}

		var userId = identity.Current.UserId;
		var now = timeProvider.GetUtcNow();
		var series = DailyCountSeries.EndingAt(request.Days, now);

		// Хранилище делит один DbContext, поэтому запросы идут строго последовательно
		var statusCounts = await storage.GetSourceStatusCountsAsync(userId, ct);
		var posts = await storage.GetPostTotalsAsync(userId, ct);
		var last24Hours = await storage.GetRunTotalsAsync(userId, now.AddHours(-24), ct);
		var last7Days = await storage.GetRunTotalsAsync(userId, now.AddDays(-7), ct);
		var last30Days = await storage.GetRunTotalsAsync(userId, now.AddDays(-30), ct);
		var period = await storage.GetRunTotalsAsync(userId, series.Since, ct);
		var byDay = await storage.GetRunsByDayAsync(userId, series.Since, ct);
		var topSources = await storage.GetTopSourcesAsync(userId, series.Since, TopSourcesLimit, ct);
		var lastRunAt = await storage.GetLastRunAtAsync(userId, ct);

		var sourcesByStatus = FillAllStatuses(statusCounts);
		return new ParseChannelStatsResponse
		{
			Sources = new ParseChannelSourceTotals
			{
				Total = sourcesByStatus.Sum(x => x.Count),
				Active = sourcesByStatus
					.Where(x => x.Status is not (ParseChannelSourceStatus.Finished or ParseChannelSourceStatus.Canceled))
					.Sum(x => x.Count),
				Failed = sourcesByStatus.Where(x => x.Status == ParseChannelSourceStatus.Failed).Sum(x => x.Count)
			},
			Posts = posts,
			Last24Hours = last24Hours,
			Last7Days = last7Days,
			Last30Days = last30Days,
			Period = period,
			LastRunAt = lastRunAt,
			SourcesByStatus = sourcesByStatus,
			PublishedByDay = series.Fill(byDay.Select(x => new DiscoverDailyCount { Date = x.Date, Count = x.Published })),
			SkippedByDay = series.Fill(byDay.Select(x => new DiscoverDailyCount { Date = x.Date, Count = x.Skipped })),
			TopSources = topSources
		};
	}

	/// <summary>
	/// Дополнить разбивку по статусам нулями, чтобы фронт всегда получал полный набор в фиксированном порядке
	/// </summary>
	/// <param name="counts"></param>
	/// <returns></returns>
	private static List<ParseChannelSourceStatusCount> FillAllStatuses(
		IReadOnlyCollection<ParseChannelSourceStatusCount> counts
	) =>
		Enum.GetValues<ParseChannelSourceStatus>()
			.Select(status => new ParseChannelSourceStatusCount
			{
				Status = status,
				Count = counts.Where(x => x.Status == status).Sum(x => x.Count)
			})
			.ToList();
}

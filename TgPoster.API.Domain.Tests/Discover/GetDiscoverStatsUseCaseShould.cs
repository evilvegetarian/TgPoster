using Moq;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.Discover;

public class GetDiscoverStatsUseCaseShould
{
	private readonly Mock<IGetDiscoverStatsStorage> storage;
	private readonly GetDiscoverStatsUseCase sut;

	public GetDiscoverStatsUseCaseShould()
	{
		storage = new Mock<IGetDiscoverStatsStorage>();
		storage.Setup(s => s.GetTotalsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(DiscoverStatsTotalsDto.Empty);
		storage.Setup(s => s.GetBannedCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
		storage.Setup(s => s.GetStatusCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetPeerTypeCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetCategoryCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetLanguageCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetParticipantsBucketCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetParsedByDayAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(s => s.GetFoundByDayAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(s => s.GetTopSourcesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
		sut = new GetDiscoverStatsUseCase(storage.Object);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(GetDiscoverStatsUseCase.MaxDays + 1)]
	public async Task ThrowInvalidPeriod_WhenDaysOutOfRange(int days)
	{
		await Should.ThrowAsync<InvalidDiscoverStatsPeriodException>(
			() => sut.Handle(new GetDiscoverStatsQuery(days), CancellationToken.None));

		storage.Verify(s => s.GetTotalsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReturnZeroTotalsAndFullBreakdowns_WhenStorageIsEmpty()
	{
		var result = await sut.Handle(new GetDiscoverStatsQuery(7), CancellationToken.None);

		result.Totals.Total.ShouldBe(0);
		result.Totals.NotParsed.ShouldBe(0);
		result.Totals.Private.ShouldBe(0);
		result.ByStatus.Count.ShouldBe(Enum.GetValues<DiscoverChannelStatus>().Length);
		result.ByStatus.ShouldAllBe(x => x.Count == 0);
		result.ByParticipants.Count.ShouldBe(Enum.GetValues<DiscoverParticipantsBucket>().Length);
		result.ByParticipants.ShouldAllBe(x => x.Count == 0);
		result.ParsedByDay.Count.ShouldBe(7);
		result.FoundByDay.Count.ShouldBe(7);
		result.ParsedByDay.ShouldAllBe(x => x.Count == 0);
	}

	[Fact]
	public async Task DeriveNotParsedAndPrivate_FromTotals()
	{
		storage.Setup(s => s.GetTotalsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(DiscoverStatsTotalsDto.Empty with { Total = 100, Parsed = 30, Public = 80 });
		storage.Setup(s => s.GetBannedCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);

		var result = await sut.Handle(new GetDiscoverStatsQuery(30), CancellationToken.None);

		result.Totals.Total.ShouldBe(100);
		result.Totals.Parsed.ShouldBe(30);
		result.Totals.NotParsed.ShouldBe(70);
		result.Totals.Public.ShouldBe(80);
		result.Totals.Private.ShouldBe(20);
		result.Totals.Banned.ShouldBe(5);
	}

	[Fact]
	public async Task PassFreshnessThrough_FromTotals()
	{
		var lastParsedAt = DateTimeOffset.UtcNow.AddHours(-2);
		storage.Setup(s => s.GetTotalsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(DiscoverStatsTotalsDto.Empty with
			{
				ParsedLast24Hours = 3,
				ParsedLast7Days = 10,
				ParsedLast30Days = 25,
				FoundLast24Hours = 40,
				FoundLast7Days = 200,
				FoundLast30Days = 900,
				LastParsedAt = lastParsedAt
			});

		var result = await sut.Handle(new GetDiscoverStatsQuery(30), CancellationToken.None);

		result.Freshness.ParsedLast24Hours.ShouldBe(3);
		result.Freshness.ParsedLast7Days.ShouldBe(10);
		result.Freshness.ParsedLast30Days.ShouldBe(25);
		result.Freshness.FoundLast24Hours.ShouldBe(40);
		result.Freshness.FoundLast7Days.ShouldBe(200);
		result.Freshness.FoundLast30Days.ShouldBe(900);
		result.Freshness.LastParsedAt.ShouldBe(lastParsedAt);
	}

	[Fact]
	public async Task FillMissingStatusesWithZero_AndKeepEnumOrder()
	{
		storage.Setup(s => s.GetStatusCountsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				new DiscoverStatusCount { Status = DiscoverChannelStatus.Completed, Count = 12 },
				new DiscoverStatusCount { Status = DiscoverChannelStatus.Pending, Count = 40 }
			]);

		var result = await sut.Handle(new GetDiscoverStatsQuery(30), CancellationToken.None);

		result.ByStatus.Select(x => x.Status)
			.ShouldBe(Enum.GetValues<DiscoverChannelStatus>());
		result.ByStatus.Single(x => x.Status == DiscoverChannelStatus.Pending).Count.ShouldBe(40);
		result.ByStatus.Single(x => x.Status == DiscoverChannelStatus.Completed).Count.ShouldBe(12);
		result.ByStatus.Single(x => x.Status == DiscoverChannelStatus.Error).Count.ShouldBe(0);
	}

	[Fact]
	public async Task FillMissingParticipantsBucketsWithZero_AndKeepEnumOrder()
	{
		storage.Setup(s => s.GetParticipantsBucketCountsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				new DiscoverParticipantsBucketCount { Bucket = DiscoverParticipantsBucket.Over100K, Count = 2 }
			]);

		var result = await sut.Handle(new GetDiscoverStatsQuery(30), CancellationToken.None);

		result.ByParticipants.Select(x => x.Bucket)
			.ShouldBe(Enum.GetValues<DiscoverParticipantsBucket>());
		result.ByParticipants.Single(x => x.Bucket == DiscoverParticipantsBucket.Over100K).Count.ShouldBe(2);
		result.ByParticipants.Single(x => x.Bucket == DiscoverParticipantsBucket.UpTo1K).Count.ShouldBe(0);
	}

	[Fact]
	public async Task ReplaceEmptyPeerTypeWithUnknown_AndSortByCountDesc()
	{
		storage.Setup(s => s.GetPeerTypeCountsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				new DiscoverNamedCount { Name = "", Count = 5 },
				new DiscoverNamedCount { Name = "chat", Count = 7 },
				new DiscoverNamedCount { Name = "channel", Count = 90 }
			]);

		var result = await sut.Handle(new GetDiscoverStatsQuery(30), CancellationToken.None);

		result.ByPeerType.Select(x => x.Name).ShouldBe(["channel", "chat", "unknown"]);
		result.ByPeerType.Single(x => x.Name == "unknown").Count.ShouldBe(5);
	}

	[Fact]
	public async Task BuildDenseDailySeries_EndingToday()
	{
		var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
		storage.Setup(s => s.GetParsedByDayAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				new DiscoverDailyCount { Date = today.AddDays(-2), Count = 4 },
				new DiscoverDailyCount { Date = today, Count = 1 }
			]);

		var result = await sut.Handle(new GetDiscoverStatsQuery(5), CancellationToken.None);

		result.ParsedByDay.Count.ShouldBe(5);
		result.ParsedByDay.Select(x => x.Date)
			.ShouldBe([today.AddDays(-4), today.AddDays(-3), today.AddDays(-2), today.AddDays(-1), today]);
		result.ParsedByDay.Select(x => x.Count).ShouldBe([0, 0, 4, 0, 1]);
	}

	[Fact]
	public async Task RequestDailySeries_FromStartOfFirstDayOfWindow()
	{
		DateTimeOffset? capturedSince = null;
		storage.Setup(s => s.GetParsedByDayAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.Callback<DateTimeOffset, CancellationToken>((since, _) => capturedSince = since)
			.ReturnsAsync([]);

		await sut.Handle(new GetDiscoverStatsQuery(3), CancellationToken.None);

		var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
		capturedSince.ShouldNotBeNull();
		capturedSince.Value.Offset.ShouldBe(TimeSpan.Zero);
		DateOnly.FromDateTime(capturedSince.Value.UtcDateTime).ShouldBe(today.AddDays(-2));
		capturedSince.Value.TimeOfDay.ShouldBe(TimeSpan.Zero);
	}

	[Fact]
	public async Task PassCategoriesLanguagesAndTopSourcesThrough()
	{
		var categories = new List<DiscoverNamedCount> { new() { Name = "Tech", Count = 3 } };
		var languages = new List<DiscoverNamedCount> { new() { Name = "ru", Count = 9 } };
		var sources = new List<DiscoverSourceStat>
		{
			new() { Id = Guid.NewGuid(), FoundCount = 50, IsBanned = false }
		};
		storage.Setup(s => s.GetCategoryCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
		storage.Setup(s => s.GetLanguageCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(languages);
		storage.Setup(s => s.GetTopSourcesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(sources);

		var result = await sut.Handle(new GetDiscoverStatsQuery(30), CancellationToken.None);

		result.ByCategory.ShouldBe(categories);
		result.ByLanguage.ShouldBe(languages);
		result.TopSources.ShouldBe(sources);
	}
}

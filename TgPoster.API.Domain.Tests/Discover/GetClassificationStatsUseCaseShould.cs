using Moq;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;
using TgPoster.Exceptions.BadRequest;

namespace TgPoster.API.Domain.Tests.Discover;

public class GetClassificationStatsUseCaseShould
{
	private readonly Mock<IGetClassificationStatsStorage> storage;
	private readonly GetClassificationStatsUseCase sut;

	public GetClassificationStatsUseCaseShould()
	{
		storage = new Mock<IGetClassificationStatsStorage>();
		storage.Setup(s => s.GetTotalsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ClassificationStatsTotalsDto.Empty);
		storage.Setup(s => s.GetCategoryStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetLanguageCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetConfidenceBucketCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetTopSubcategoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		storage.Setup(s => s.GetDistinctSubcategoryCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
		storage.Setup(s => s.GetTopTagsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
		storage.Setup(s => s.GetDistinctTagCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
		storage.Setup(s => s.GetClassifiedByDayAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);
		sut = new GetClassificationStatsUseCase(storage.Object);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(GetClassificationStatsUseCase.MaxDays + 1)]
	public async Task ThrowInvalidPeriod_WhenDaysOutOfRange(int days)
	{
		await Should.ThrowAsync<InvalidDiscoverStatsPeriodException>(
			() => sut.Handle(new GetClassificationStatsQuery(days), CancellationToken.None));

		storage.Verify(s => s.GetTotalsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ReturnZeroTotalsAndFullBreakdowns_WhenStorageIsEmpty()
	{
		var result = await sut.Handle(new GetClassificationStatsQuery(7), CancellationToken.None);

		result.Totals.Total.ShouldBe(0);
		result.Totals.Skipped.ShouldBe(0);
		result.Totals.AverageConfidence.ShouldBeNull();
		result.Freshness.LastClassifiedAt.ShouldBeNull();
		result.ByConfidence.Count.ShouldBe(Enum.GetValues<ClassificationConfidenceBucket>().Length);
		result.ByConfidence.ShouldAllBe(x => x.Count == 0);
		result.ClassifiedByDay.Count.ShouldBe(7);
		result.ClassifiedByDay.ShouldAllBe(x => x.Count == 0);
	}

	[Fact]
	public async Task DeriveSkipped_AndPassTotalsThrough()
	{
		storage.Setup(s => s.GetTotalsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ClassificationStatsTotalsDto.Empty with
			{
				Total = 100,
				Eligible = 80,
				Classified = 50,
				Pending = 30,
				WithCategory = 48,
				WithTags = 45,
				AverageConfidence = 0.82
			});
		storage.Setup(s => s.GetDistinctSubcategoryCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(17);
		storage.Setup(s => s.GetDistinctTagCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(120);

		var result = await sut.Handle(new GetClassificationStatsQuery(30), CancellationToken.None);

		result.Totals.Total.ShouldBe(100);
		result.Totals.Eligible.ShouldBe(80);
		result.Totals.Skipped.ShouldBe(20);
		result.Totals.Classified.ShouldBe(50);
		result.Totals.Pending.ShouldBe(30);
		result.Totals.WithCategory.ShouldBe(48);
		result.Totals.WithTags.ShouldBe(45);
		result.Totals.AverageConfidence.ShouldBe(0.82);
		result.Totals.DistinctSubcategories.ShouldBe(17);
		result.Totals.DistinctTags.ShouldBe(120);
	}

	[Fact]
	public async Task PassFreshnessThrough_FromTotals()
	{
		var lastClassifiedAt = DateTimeOffset.UtcNow.AddMinutes(-20);
		storage.Setup(s => s.GetTotalsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ClassificationStatsTotalsDto.Empty with
			{
				ClassifiedLast24Hours = 6,
				ClassifiedLast7Days = 40,
				ClassifiedLast30Days = 150,
				LastClassifiedAt = lastClassifiedAt
			});

		var result = await sut.Handle(new GetClassificationStatsQuery(30), CancellationToken.None);

		result.Freshness.ClassifiedLast24Hours.ShouldBe(6);
		result.Freshness.ClassifiedLast7Days.ShouldBe(40);
		result.Freshness.ClassifiedLast30Days.ShouldBe(150);
		result.Freshness.LastClassifiedAt.ShouldBe(lastClassifiedAt);
	}

	[Fact]
	public async Task FillMissingConfidenceBucketsWithZero_AndKeepEnumOrder()
	{
		storage.Setup(s => s.GetConfidenceBucketCountsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				new ClassificationConfidenceBucketCount { Bucket = ClassificationConfidenceBucket.Over90, Count = 12 },
				new ClassificationConfidenceBucketCount { Bucket = ClassificationConfidenceBucket.UpTo50, Count = 3 }
			]);

		var result = await sut.Handle(new GetClassificationStatsQuery(30), CancellationToken.None);

		result.ByConfidence.Select(x => x.Bucket).ShouldBe(Enum.GetValues<ClassificationConfidenceBucket>());
		result.ByConfidence.Single(x => x.Bucket == ClassificationConfidenceBucket.Over90).Count.ShouldBe(12);
		result.ByConfidence.Single(x => x.Bucket == ClassificationConfidenceBucket.UpTo50).Count.ShouldBe(3);
		result.ByConfidence.Single(x => x.Bucket == ClassificationConfidenceBucket.From70To80).Count.ShouldBe(0);
	}

	[Fact]
	public async Task BuildDenseDailySeries_EndingToday()
	{
		var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
		storage.Setup(s => s.GetClassifiedByDayAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(
			[
				new DiscoverDailyCount { Date = today.AddDays(-1), Count = 5 },
				new DiscoverDailyCount { Date = today, Count = 2 }
			]);

		var result = await sut.Handle(new GetClassificationStatsQuery(3), CancellationToken.None);

		result.ClassifiedByDay.Select(x => x.Date).ShouldBe([today.AddDays(-2), today.AddDays(-1), today]);
		result.ClassifiedByDay.Select(x => x.Count).ShouldBe([0, 5, 2]);
	}

	[Fact]
	public async Task RequestDailySeries_FromStartOfFirstDayOfWindow()
	{
		DateTimeOffset? capturedSince = null;
		storage.Setup(s => s.GetClassifiedByDayAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
			.Callback<DateTimeOffset, CancellationToken>((since, _) => capturedSince = since)
			.ReturnsAsync([]);

		await sut.Handle(new GetClassificationStatsQuery(4), CancellationToken.None);

		var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
		capturedSince.ShouldNotBeNull();
		capturedSince.Value.Offset.ShouldBe(TimeSpan.Zero);
		DateOnly.FromDateTime(capturedSince.Value.UtcDateTime).ShouldBe(today.AddDays(-3));
		capturedSince.Value.TimeOfDay.ShouldBe(TimeSpan.Zero);
	}

	[Fact]
	public async Task PassBreakdownsThrough_AndRequestLimitedTops()
	{
		var categories = new List<ClassificationCategoryStat>
		{
			new() { Name = "Технологии", Count = 10, AverageConfidence = 0.9 }
		};
		var languages = new List<DiscoverNamedCount> { new() { Name = "ru", Count = 9 } };
		var subcategories = new List<ClassificationSubcategoryStat>
		{
			new() { Category = "Технологии", Subcategory = "AI и ML", Count = 4 }
		};
		var tags = new List<DiscoverNamedCount> { new() { Name = "нейросети", Count = 7 } };
		storage.Setup(s => s.GetCategoryStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
		storage.Setup(s => s.GetLanguageCountsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(languages);
		storage.Setup(s => s.GetTopSubcategoriesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(subcategories);
		storage.Setup(s => s.GetTopTagsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(tags);

		var result = await sut.Handle(new GetClassificationStatsQuery(30), CancellationToken.None);

		result.ByCategory.ShouldBe(categories);
		result.ByLanguage.ShouldBe(languages);
		result.TopSubcategories.ShouldBe(subcategories);
		result.TopTags.ShouldBe(tags);
		storage.Verify(s => s.GetTopSubcategoriesAsync(It.Is<int>(x => x > 0), It.IsAny<CancellationToken>()), Times.Once);
		storage.Verify(s => s.GetTopTagsAsync(It.Is<int>(x => x > 0), It.IsAny<CancellationToken>()), Times.Once);
	}
}

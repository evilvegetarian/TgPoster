using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Worker.Domain;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ClassificationStatsStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ClassificationStatsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetTotalsAsync_ShouldCountEligibleClassifiedPendingAndTags()
	{
		var before = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => Classify(c, "Tech", 0.9, ["ai"])),
			NewChannel(c => Classify(c, null, 0.3, [])),
			NewChannel(),
			NewChannel(c => { c.LastClassificationAttemptAt = DateTimeOffset.UtcNow; }),
			NewChannel(c => { c.Username = null; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);

		(after.Total - before.Total).ShouldBe(5);
		(after.Eligible - before.Eligible).ShouldBe(4);
		(after.Classified - before.Classified).ShouldBe(2);
		(after.Pending - before.Pending).ShouldBe(2);
		(after.Failed - before.Failed).ShouldBe(1);
		(after.WithCategory - before.WithCategory).ShouldBe(1);
		(after.WithTags - before.WithTags).ShouldBe(1);
		after.AverageConfidence.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetTotalsAsync_ShouldCountFreshnessWindowsRelativeToNow()
	{
		var now = DateTimeOffset.UtcNow;
		var before = await sut.GetTotalsAsync(now, CancellationToken.None);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { Classify(c, "Tech", 0.9, []); c.LastClassifiedAt = now.AddHours(-2); }),
			NewChannel(c => { Classify(c, "Tech", 0.9, []); c.LastClassifiedAt = now.AddDays(-3); }),
			NewChannel(c => { Classify(c, "Tech", 0.9, []); c.LastClassifiedAt = now.AddDays(-20); }),
			NewChannel(c => { Classify(c, "Tech", 0.9, []); c.LastClassifiedAt = now.AddDays(-60); }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetTotalsAsync(now, CancellationToken.None);

		(after.ClassifiedLast24Hours - before.ClassifiedLast24Hours).ShouldBe(1);
		(after.ClassifiedLast7Days - before.ClassifiedLast7Days).ShouldBe(2);
		(after.ClassifiedLast30Days - before.ClassifiedLast30Days).ShouldBe(3);
		after.LastClassifiedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetTotalsAsync_ShouldIgnoreBannedChannels()
	{
		var before = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);
		context.DiscoveredChannels.Add(NewChannel(c =>
		{
			Classify(c, "Tech", 0.9, []);
			c.IsBanned = true;
		}));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);

		after.Total.ShouldBe(before.Total);
		after.Classified.ShouldBe(before.Classified);
	}

	[Fact]
	public async Task GetCategoryStatsAsync_ShouldCountAndAverageConfidence_OrderedByCountDesc()
	{
		var big = $"big_{Guid.NewGuid():N}";
		var small = $"small_{Guid.NewGuid():N}";
		context.DiscoveredChannels.AddRange(
			NewChannel(c => Classify(c, big, 0.6, [])),
			NewChannel(c => Classify(c, big, 0.8, [])),
			NewChannel(c => Classify(c, big, 1.0, [])),
			NewChannel(c => Classify(c, small, 0.4, [])));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetCategoryStatsAsync(CancellationToken.None);

		var bigStat = result.Single(x => x.Name == big);
		bigStat.Count.ShouldBe(3);
		bigStat.AverageConfidence.ShouldNotBeNull();
		bigStat.AverageConfidence.Value.ShouldBe(0.8, 0.0001);
		result.Single(x => x.Name == small).Count.ShouldBe(1);
		result.FindIndex(x => x.Name == big).ShouldBeLessThan(result.FindIndex(x => x.Name == small));
		result.Select(x => x.Count).ShouldBe(result.Select(x => x.Count).OrderByDescending(x => x));
	}

	[Fact]
	public async Task GetLanguageCountsAsync_ShouldGroupByLanguage()
	{
		var language = Guid.NewGuid().ToString("N")[..6];
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.Language = language; }),
			NewChannel(c => { c.Language = language; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetLanguageCountsAsync(CancellationToken.None);

		result.Single(x => x.Name == language).Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetConfidenceBucketCountsAsync_ShouldPutClassifiedChannelsIntoRightBuckets()
	{
		var before = await sut.GetConfidenceBucketCountsAsync(CancellationToken.None);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => Classify(c, "Tech", null, [])),
			NewChannel(c => Classify(c, "Tech", 0.49, [])),
			NewChannel(c => Classify(c, "Tech", 0.5, [])),
			NewChannel(c => Classify(c, "Tech", 0.7, [])),
			NewChannel(c => Classify(c, "Tech", 0.85, [])),
			NewChannel(c => Classify(c, "Tech", 0.9, [])),
			NewChannel(c => Classify(c, "Tech", 1.0, [])),
			NewChannel(c => { c.ClassificationConfidence = 0.95; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetConfidenceBucketCountsAsync(CancellationToken.None);

		after.Select(x => x.Bucket).ShouldBe(Enum.GetValues<ClassificationConfidenceBucket>());
		Delta(after, before, ClassificationConfidenceBucket.Unknown).ShouldBe(1);
		Delta(after, before, ClassificationConfidenceBucket.UpTo50).ShouldBe(1);
		Delta(after, before, ClassificationConfidenceBucket.From50To70).ShouldBe(1);
		Delta(after, before, ClassificationConfidenceBucket.From70To80).ShouldBe(1);
		Delta(after, before, ClassificationConfidenceBucket.From80To90).ShouldBe(1);
		Delta(after, before, ClassificationConfidenceBucket.Over90).ShouldBe(2);
	}

	[Fact]
	public async Task GetTopSubcategoriesAsync_ShouldGroupByCategoryAndSubcategory()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { Classify(c, $"A {marker}", 0.9, []); c.Subcategory = $"Sub {marker}"; }),
			NewChannel(c => { Classify(c, $"A {marker}", 0.9, []); c.Subcategory = $"Sub {marker}"; }),
			NewChannel(c => { Classify(c, $"B {marker}", 0.9, []); c.Subcategory = $"Sub {marker}"; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetTopSubcategoriesAsync(int.MaxValue, CancellationToken.None);

		result.Single(x => x.Category == $"A {marker}" && x.Subcategory == $"Sub {marker}").Count.ShouldBe(2);
		result.Single(x => x.Category == $"B {marker}" && x.Subcategory == $"Sub {marker}").Count.ShouldBe(1);
		result.Select(x => x.Count).ShouldBe(result.Select(x => x.Count).OrderByDescending(x => x));
	}

	[Fact]
	public async Task GetTopSubcategoriesAsync_ShouldRespectLimit()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.Subcategory = $"One {marker}"; }),
			NewChannel(c => { c.Subcategory = $"Two {marker}"; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetTopSubcategoriesAsync(1, CancellationToken.None);

		result.Count.ShouldBe(1);
	}

	[Fact]
	public async Task GetDistinctSubcategoryCountAsync_ShouldCountCategorySubcategoryPairs()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		var before = await sut.GetDistinctSubcategoryCountAsync(CancellationToken.None);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.Category = $"A {marker}"; c.Subcategory = $"Sub {marker}"; }),
			NewChannel(c => { c.Category = $"A {marker}"; c.Subcategory = $"Sub {marker}"; }),
			NewChannel(c => { c.Category = $"B {marker}"; c.Subcategory = $"Sub {marker}"; }),
			NewChannel(c => { c.Category = $"A {marker}"; c.Subcategory = null; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetDistinctSubcategoryCountAsync(CancellationToken.None);

		(after - before).ShouldBe(2);
	}

	[Fact]
	public async Task GetTopTagsAsync_ShouldMergeCaseAndSkipBlankTags()
	{
		var tag = $"tag{Guid.NewGuid():N}"[..14];
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.Tags = [tag, " "]; }),
			NewChannel(c => { c.Tags = [tag.ToUpperInvariant()]; }),
			NewChannel(c => { c.Tags = [$" {tag} "]; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetTopTagsAsync(int.MaxValue, CancellationToken.None);

		result.Single(x => x.Name == tag).Count.ShouldBe(3);
		result.ShouldNotContain(x => x.Name == "" || x.Name == tag.ToUpperInvariant());
		result.Select(x => x.Count).ShouldBe(result.Select(x => x.Count).OrderByDescending(x => x));
	}

	[Fact]
	public async Task GetDistinctTagCountAsync_ShouldCountTagsIgnoringCase()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		var before = await sut.GetDistinctTagCountAsync(CancellationToken.None);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.Tags = [$"a{marker}", $"b{marker}"]; }),
			NewChannel(c => { c.Tags = [$"A{marker}"]; }),
			NewChannel(c => { c.Tags = null; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetDistinctTagCountAsync(CancellationToken.None);

		(after - before).ShouldBe(2);
	}

	[Fact]
	public async Task GetClassifiedByDayAsync_ShouldGroupByUtcDayAndRespectSince()
	{
		var day = new DateTimeOffset(2001, 2, 10, 23, 30, 0, TimeSpan.Zero);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.LastClassifiedAt = day; }),
			NewChannel(c => { c.LastClassifiedAt = day.AddMinutes(20); }),
			NewChannel(c => { c.LastClassifiedAt = day.AddMinutes(40); }),
			NewChannel(c => { c.LastClassifiedAt = day.AddDays(-5); }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetClassifiedByDayAsync(day.AddDays(-1), CancellationToken.None);

		result.Single(x => x.Date == new DateOnly(2001, 2, 10)).Count.ShouldBe(2);
		result.Single(x => x.Date == new DateOnly(2001, 2, 11)).Count.ShouldBe(1);
		result.ShouldNotContain(x => x.Date == new DateOnly(2001, 2, 5));
		result.Select(x => x.Date).ShouldBe(result.Select(x => x.Date).OrderBy(x => x));
	}

	[Fact]
	public async Task GetClassificationHistoryAsync_ShouldReturnOnlyClassified_NewestFirst()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		var older = NewChannel(c =>
		{
			c.Title = $"Older {marker}";
			Classify(c, "Крипто", 0.75, ["биткоин", "трейдинг"]);
			c.Subcategory = "Трейдинг";
			c.Language = "ru";
			c.LastClassifiedAt = DateTimeOffset.UtcNow.AddDays(-2);
		});
		var newer = NewChannel(c =>
		{
			c.Title = $"Newer {marker}";
			Classify(c, "Новости", 0.95, []);
			c.Tags = null;
			c.LastClassifiedAt = DateTimeOffset.UtcNow.AddHours(-1);
		});
		var notClassified = NewChannel(c => { c.Title = $"NotClassified {marker}"; });
		context.DiscoveredChannels.AddRange(older, newer, notClassified);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetClassificationHistoryAsync(
			HistoryQuery(marker), CancellationToken.None);

		result.TotalCount.ShouldBe(2);
		result.Items.Select(x => x.Id).ShouldBe([newer.Id, older.Id]);
		var olderItem = result.Items.Single(x => x.Id == older.Id);
		olderItem.Category.ShouldBe("Крипто");
		olderItem.Subcategory.ShouldBe("Трейдинг");
		olderItem.Tags.ShouldBe(["биткоин", "трейдинг"]);
		olderItem.Language.ShouldBe("ru");
		olderItem.Confidence.ShouldBe(0.75);
		result.Items.Single(x => x.Id == newer.Id).Tags.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetClassificationHistoryAsync_ShouldFilterByCategoryAndConfidence()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		var category = $"Cat {marker}";
		var confident = NewChannel(c => { c.Title = $"A {marker}"; Classify(c, category, 0.95, []); });
		var unsure = NewChannel(c => { c.Title = $"B {marker}"; Classify(c, category, 0.3, []); });
		var otherCategory = NewChannel(c => { c.Title = $"C {marker}"; Classify(c, "Other", 0.95, []); });
		context.DiscoveredChannels.AddRange(confident, unsure, otherCategory);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var byCategory = await sut.GetClassificationHistoryAsync(
			HistoryQuery(marker) with { Category = category }, CancellationToken.None);
		var byCategoryAndLowConfidence = await sut.GetClassificationHistoryAsync(
			HistoryQuery(marker) with { Category = category, Confidence = ClassificationConfidenceBucket.UpTo50 },
			CancellationToken.None);

		byCategory.Items.Select(x => x.Id).ShouldBe([confident.Id, unsure.Id], true);
		byCategoryAndLowConfidence.Items.Select(x => x.Id).ShouldBe([unsure.Id]);
	}

	[Fact]
	public async Task GetClassificationHistoryAsync_ShouldFilterByClassifiedAtPeriod()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		var now = DateTimeOffset.UtcNow;
		var inside = NewChannel(c => { c.Title = $"Inside {marker}"; c.LastClassifiedAt = now.AddDays(-3); });
		var tooOld = NewChannel(c => { c.Title = $"Old {marker}"; c.LastClassifiedAt = now.AddDays(-30); });
		var tooNew = NewChannel(c => { c.Title = $"New {marker}"; c.LastClassifiedAt = now; });
		context.DiscoveredChannels.AddRange(inside, tooOld, tooNew);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetClassificationHistoryAsync(
			HistoryQuery(marker) with { From = now.AddDays(-7), To = now.AddDays(-1) },
			CancellationToken.None);

		result.Items.Select(x => x.Id).ShouldBe([inside.Id]);
	}

	[Fact]
	public async Task GetClassificationHistoryAsync_ShouldPaginate()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		for (var i = 0; i < 5; i++)
		{
			var minutesAgo = i;
			context.DiscoveredChannels.Add(NewChannel(c =>
			{
				c.Title = $"Page {marker} {minutesAgo}";
				c.LastClassifiedAt = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo);
			}));
		}

		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var page1 = await sut.GetClassificationHistoryAsync(
			HistoryQuery(marker) with { PageSize = 2 }, CancellationToken.None);
		var page3 = await sut.GetClassificationHistoryAsync(
			HistoryQuery(marker) with { Page = 3, PageSize = 2 }, CancellationToken.None);

		page1.TotalCount.ShouldBe(5);
		page1.Items.Count.ShouldBe(2);
		page3.Items.Count.ShouldBe(1);
		page1.Items.Select(x => x.Id).ShouldNotContain(page3.Items[0].Id);
	}

	[Fact]
	public async Task GetClassificationJobStateAsync_ShouldReturnClassifierJobState()
	{
		var state = await context.WorkerJobStates
			.FirstOrDefaultAsync(x => x.JobName == WorkerJobNames.ClassifyChannels, CancellationToken.None);
		if (state is null)
		{
			state = new WorkerJobState { Id = Guid.NewGuid(), JobName = WorkerJobNames.ClassifyChannels };
			context.WorkerJobStates.Add(state);
		}

		state.Status = WorkerJobStatus.Failed;
		state.LastError = "Не задан ключ OpenRouter";
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetClassificationJobStateAsync(CancellationToken.None);

		result.ShouldNotBeNull();
		result.Status.ShouldBe(WorkerJobStateStatus.Failed);
		result.LastError.ShouldBe("Не задан ключ OpenRouter");
	}

	private static GetClassificationHistoryQuery HistoryQuery(string search) =>
		new(1, 50, search, null, null, null, null);

	private static void Classify(DiscoveredChannel channel, string? category, double? confidence, string[] tags)
	{
		channel.Category = category;
		channel.ClassificationConfidence = confidence;
		channel.Tags = tags;
		channel.LastClassifiedAt = DateTimeOffset.UtcNow;
	}

	private static DiscoveredChannel NewChannel(Action<DiscoveredChannel>? setup = null)
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"cls_{Guid.NewGuid():N}",
			Title = "Channel",
			PeerType = "channel",
			Status = DiscoveryStatus.Pending
		};
		setup?.Invoke(channel);
		return channel;
	}

	private static int Delta(
		IEnumerable<ClassificationConfidenceBucketCount> after,
		IEnumerable<ClassificationConfidenceBucketCount> before,
		ClassificationConfidenceBucket bucket
	) =>
		after.Where(x => x.Bucket == bucket).Sum(x => x.Count)
		- before.Where(x => x.Bucket == bucket).Sum(x => x.Count);
}

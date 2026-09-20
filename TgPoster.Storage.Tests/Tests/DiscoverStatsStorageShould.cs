using Shouldly;
using TgPoster.API.Domain.UseCases.Discover;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DiscoverStatsStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly DiscoverStatsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetTotalsAsync_ShouldCountParsedPublicClassifiedAndParticipants()
	{
		var before = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);
		var parsedPublic = NewChannel(c =>
		{
			c.LastDiscoveredAt = DateTimeOffset.UtcNow.AddHours(-1);
			c.Category = "Tech";
			c.ParticipantsCount = 1500;
		});
		var privateNotParsed = NewChannel(c => { c.Username = null; c.ParticipantsCount = 500; });
		context.DiscoveredChannels.AddRange(parsedPublic, privateNotParsed);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);

		(after.Total - before.Total).ShouldBe(2);
		(after.Parsed - before.Parsed).ShouldBe(1);
		(after.Public - before.Public).ShouldBe(1);
		(after.Classified - before.Classified).ShouldBe(1);
		(after.WithParticipants - before.WithParticipants).ShouldBe(2);
		(after.TotalParticipants - before.TotalParticipants).ShouldBe(2000);
	}

	[Fact]
	public async Task GetTotalsAsync_ShouldCountFreshnessWindowsRelativeToNow()
	{
		var now = DateTimeOffset.UtcNow;
		var before = await sut.GetTotalsAsync(now, CancellationToken.None);
		var parsedToday = NewChannel(c => { c.LastDiscoveredAt = now.AddHours(-2); });
		var parsedThisWeek = NewChannel(c => { c.LastDiscoveredAt = now.AddDays(-3); });
		var parsedThisMonth = NewChannel(c => { c.LastDiscoveredAt = now.AddDays(-20); });
		var parsedLongAgo = NewChannel(c => { c.LastDiscoveredAt = now.AddDays(-60); });
		context.DiscoveredChannels.AddRange(parsedToday, parsedThisWeek, parsedThisMonth, parsedLongAgo);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetTotalsAsync(now, CancellationToken.None);

		(after.ParsedLast24Hours - before.ParsedLast24Hours).ShouldBe(1);
		(after.ParsedLast7Days - before.ParsedLast7Days).ShouldBe(2);
		(after.ParsedLast30Days - before.ParsedLast30Days).ShouldBe(3);
		(after.FoundLast24Hours - before.FoundLast24Hours).ShouldBe(4);
		after.LastParsedAt.ShouldNotBeNull();
		after.LastParsedAt.Value.ShouldBeGreaterThanOrEqualTo(parsedToday.LastDiscoveredAt!.Value.AddSeconds(-1));
		after.FirstFoundAt.ShouldNotBeNull();
		after.LastFoundAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetTotalsAsync_ShouldReturnEmpty_WhenNoChannels()
	{
		using var emptyContext = fixture.GetDbContext();
		var hasAny = emptyContext.DiscoveredChannels.Any();

		var totals = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);

		if (!hasAny)
		{
			totals.ShouldBe(DiscoverStatsTotalsDto.Empty);
		}
		else
		{
			totals.Total.ShouldBeGreaterThan(0);
		}
	}

	[Fact]
	public async Task GetBannedCountAsync_ShouldCountBannedDespiteQueryFilter()
	{
		var before = await sut.GetBannedCountAsync(CancellationToken.None);
		var banned = NewChannel(c => { c.IsBanned = true; });
		var totalsBefore = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);
		context.DiscoveredChannels.Add(banned);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetBannedCountAsync(CancellationToken.None);
		var totalsAfter = await sut.GetTotalsAsync(DateTimeOffset.UtcNow, CancellationToken.None);

		(after - before).ShouldBe(1);
		totalsAfter.Total.ShouldBe(totalsBefore.Total);
	}

	[Fact]
	public async Task GetStatusCountsAsync_ShouldGroupByStatus()
	{
		var before = await sut.GetStatusCountsAsync(CancellationToken.None);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.Status = DiscoveryStatus.Error; }),
			NewChannel(c => { c.Status = DiscoveryStatus.Error; }),
			NewChannel(c => { c.Status = DiscoveryStatus.Skipped; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetStatusCountsAsync(CancellationToken.None);

		(CountOf(after, DiscoverChannelStatus.Error) - CountOf(before, DiscoverChannelStatus.Error)).ShouldBe(2);
		(CountOf(after, DiscoverChannelStatus.Skipped) - CountOf(before, DiscoverChannelStatus.Skipped)).ShouldBe(1);
	}

	[Fact]
	public async Task GetPeerTypeCountsAsync_ShouldReturnEmptyNameForNullPeerType()
	{
		var before = await sut.GetPeerTypeCountsAsync(CancellationToken.None);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.PeerType = "chat"; }),
			NewChannel(c => { c.PeerType = null; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetPeerTypeCountsAsync(CancellationToken.None);

		(CountOf(after, "chat") - CountOf(before, "chat")).ShouldBe(1);
		(CountOf(after, string.Empty) - CountOf(before, string.Empty)).ShouldBe(1);
	}

	[Fact]
	public async Task GetCategoryCountsAsync_ShouldSkipNullAndOrderByCountDesc()
	{
		var big = $"big_{Guid.NewGuid():N}";
		var small = $"small_{Guid.NewGuid():N}";
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.Category = big; }),
			NewChannel(c => { c.Category = big; }),
			NewChannel(c => { c.Category = big; }),
			NewChannel(c => { c.Category = small; }),
			NewChannel(c => { c.Category = null; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetCategoryCountsAsync(CancellationToken.None);

		CountOf(result, big).ShouldBe(3);
		CountOf(result, small).ShouldBe(1);
		result.ShouldNotContain(x => x.Name == string.Empty);
		result.Select(x => x.Count).ShouldBe(result.Select(x => x.Count).OrderByDescending(x => x));
		result.FindIndex(x => x.Name == big).ShouldBeLessThan(result.FindIndex(x => x.Name == small));
	}

	[Fact]
	public async Task GetLanguageCountsAsync_ShouldGroupByLanguage()
	{
		var language = Guid.NewGuid().ToString("N")[..6];
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.Language = language; }),
			NewChannel(c => { c.Language = language; }),
			NewChannel(c => { c.Language = null; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetLanguageCountsAsync(CancellationToken.None);

		CountOf(result, language).ShouldBe(2);
	}

	[Fact]
	public async Task GetParticipantsBucketCountsAsync_ShouldPutChannelsIntoRightBuckets()
	{
		var before = await sut.GetParticipantsBucketCountsAsync(CancellationToken.None);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.ParticipantsCount = null; }),
			NewChannel(c => { c.ParticipantsCount = 999; }),
			NewChannel(c => { c.ParticipantsCount = 1_000; }),
			NewChannel(c => { c.ParticipantsCount = 9_999; }),
			NewChannel(c => { c.ParticipantsCount = 10_000; }),
			NewChannel(c => { c.ParticipantsCount = 100_000; }),
			NewChannel(c => { c.ParticipantsCount = 5_000_000; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetParticipantsBucketCountsAsync(CancellationToken.None);

		after.Select(x => x.Bucket).ShouldBe(Enum.GetValues<DiscoverParticipantsBucket>());
		Delta(after, before, DiscoverParticipantsBucket.Unknown).ShouldBe(1);
		Delta(after, before, DiscoverParticipantsBucket.UpTo1K).ShouldBe(1);
		Delta(after, before, DiscoverParticipantsBucket.From1KTo10K).ShouldBe(2);
		Delta(after, before, DiscoverParticipantsBucket.From10KTo100K).ShouldBe(1);
		Delta(after, before, DiscoverParticipantsBucket.Over100K).ShouldBe(2);
	}

	[Fact]
	public async Task GetParsedByDayAsync_ShouldGroupByUtcDayAndRespectSince()
	{
		var day = new DateTimeOffset(2000, 1, 10, 23, 30, 0, TimeSpan.Zero);
		context.DiscoveredChannels.AddRange(
			NewChannel(c => { c.LastDiscoveredAt = day; }),
			NewChannel(c => { c.LastDiscoveredAt = day.AddMinutes(20); }),
			NewChannel(c => { c.LastDiscoveredAt = day.AddMinutes(40); }),
			NewChannel(c => { c.LastDiscoveredAt = day.AddDays(-5); }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetParsedByDayAsync(day.AddDays(-1), CancellationToken.None);

		result.Single(x => x.Date == new DateOnly(2000, 1, 10)).Count.ShouldBe(2);
		result.Single(x => x.Date == new DateOnly(2000, 1, 11)).Count.ShouldBe(1);
		result.ShouldNotContain(x => x.Date == new DateOnly(2000, 1, 5));
		result.Select(x => x.Date).ShouldBe(result.Select(x => x.Date).OrderBy(x => x));
	}

	[Fact]
	public async Task GetFoundByDayAsync_ShouldCountChannelsCreatedSince()
	{
		var before = await sut.GetFoundByDayAsync(DateTimeOffset.UtcNow.AddDays(-1), CancellationToken.None);
		context.DiscoveredChannels.AddRange(NewChannel(), NewChannel());
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var after = await sut.GetFoundByDayAsync(DateTimeOffset.UtcNow.AddDays(-1), CancellationToken.None);

		(after.Sum(x => x.Count) - before.Sum(x => x.Count)).ShouldBe(2);
		after.ShouldContain(x => x.Date == DateOnly.FromDateTime(DateTime.UtcNow));
	}

	[Fact]
	public async Task GetTopSourcesAsync_ShouldOrderByFoundCountAndIncludeBannedSource()
	{
		var richSource = NewChannel(c => { c.Title = "Rich"; c.LastDiscoveredAt = DateTimeOffset.UtcNow; });
		var bannedSource = NewChannel(c => { c.Title = "Banned"; c.IsBanned = true; });
		var poorSource = NewChannel(c => { c.Title = "Poor"; });
		context.DiscoveredChannels.AddRange(richSource, bannedSource, poorSource);
		for (var i = 0; i < 5; i++)
		{
			context.DiscoveredChannels.Add(NewChannel(c => { c.DiscoveredFromChannelId = richSource.Id; }));
		}

		for (var i = 0; i < 3; i++)
		{
			context.DiscoveredChannels.Add(NewChannel(c => { c.DiscoveredFromChannelId = bannedSource.Id; }));
		}

		context.DiscoveredChannels.Add(NewChannel(c => { c.DiscoveredFromChannelId = poorSource.Id; }));
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetTopSourcesAsync(100, CancellationToken.None);

		var rich = result.Single(x => x.Id == richSource.Id);
		var banned = result.Single(x => x.Id == bannedSource.Id);
		var poor = result.Single(x => x.Id == poorSource.Id);
		rich.FoundCount.ShouldBe(5);
		rich.Title.ShouldBe("Rich");
		rich.LastParsedAt.ShouldNotBeNull();
		rich.IsBanned.ShouldBeFalse();
		banned.FoundCount.ShouldBe(3);
		banned.Title.ShouldBe("Banned");
		banned.IsBanned.ShouldBeTrue();
		poor.FoundCount.ShouldBe(1);
		result.FindIndex(x => x.Id == richSource.Id).ShouldBeLessThan(result.FindIndex(x => x.Id == bannedSource.Id));
		result.FindIndex(x => x.Id == bannedSource.Id).ShouldBeLessThan(result.FindIndex(x => x.Id == poorSource.Id));
	}

	[Fact]
	public async Task GetTopSourcesAsync_ShouldRespectLimit()
	{
		var sources = Enumerable.Range(0, 3).Select(_ => NewChannel()).ToList();
		context.DiscoveredChannels.AddRange(sources);
		foreach (var source in sources)
		{
			context.DiscoveredChannels.Add(NewChannel(c => { c.DiscoveredFromChannelId = source.Id; }));
		}

		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetTopSourcesAsync(2, CancellationToken.None);

		result.Count.ShouldBe(2);
	}

	[Fact]
	public async Task GetParseHistoryAsync_ShouldReturnOnlyParsedChannels_NewestFirst_WithFoundCountAndSource()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		var source = NewChannel(c => { c.Title = $"Source {marker}"; });
		var older = NewChannel(c =>
		{
			c.Title = $"Older {marker}";
			c.LastDiscoveredAt = DateTimeOffset.UtcNow.AddDays(-2);
			c.LastParsedId = 100;
			c.DiscoveredFromChannelId = source.Id;
		});
		var newer = NewChannel(c =>
		{
			c.Title = $"Newer {marker}";
			c.LastDiscoveredAt = DateTimeOffset.UtcNow.AddHours(-1);
			c.Status = DiscoveryStatus.Completed;
		});
		var notParsed = NewChannel(c => { c.Title = $"NotParsed {marker}"; });
		var child = NewChannel(c => { c.DiscoveredFromChannelId = older.Id; });
		context.DiscoveredChannels.AddRange(source, older, newer, notParsed, child);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetParseHistoryAsync(
			new GetDiscoverParseHistoryQuery(1, 50, marker, null, null), CancellationToken.None);

		result.TotalCount.ShouldBe(2);
		result.Items.Select(x => x.Id).ShouldBe([newer.Id, older.Id]);
		result.Items.ShouldNotContain(x => x.Id == notParsed.Id);
		var olderItem = result.Items.Single(x => x.Id == older.Id);
		olderItem.FoundCount.ShouldBe(1);
		olderItem.LastParsedMessageId.ShouldBe(100);
		olderItem.SourceTitle.ShouldBe($"Source {marker}");
		olderItem.SourceUsername.ShouldBe(source.Username);
		olderItem.FoundAt.ShouldNotBeNull();
		result.Items.Single(x => x.Id == newer.Id).Status.ShouldBe(DiscoverChannelStatus.Completed);
	}

	[Fact]
	public async Task GetParseHistoryAsync_ShouldFilterByParsedAtPeriod()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		var now = DateTimeOffset.UtcNow;
		var inside = NewChannel(c => { c.Title = $"Inside {marker}"; c.LastDiscoveredAt = now.AddDays(-3); });
		var tooOld = NewChannel(c => { c.Title = $"Old {marker}"; c.LastDiscoveredAt = now.AddDays(-30); });
		var tooNew = NewChannel(c => { c.Title = $"New {marker}"; c.LastDiscoveredAt = now; });
		context.DiscoveredChannels.AddRange(inside, tooOld, tooNew);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetParseHistoryAsync(
			new GetDiscoverParseHistoryQuery(1, 50, marker, now.AddDays(-7), now.AddDays(-1)),
			CancellationToken.None);

		result.Items.Select(x => x.Id).ShouldBe([inside.Id]);
	}

	[Fact]
	public async Task GetParseHistoryAsync_ShouldPaginate()
	{
		var marker = Guid.NewGuid().ToString("N")[..8];
		for (var i = 0; i < 5; i++)
		{
			context.DiscoveredChannels.Add(NewChannel(c =>
			{
				c.Title = $"Page {marker} {i}";
				c.LastDiscoveredAt = DateTimeOffset.UtcNow.AddMinutes(-i);
			}));
		}

		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var page1 = await sut.GetParseHistoryAsync(
			new GetDiscoverParseHistoryQuery(1, 2, marker, null, null), CancellationToken.None);
		var page3 = await sut.GetParseHistoryAsync(
			new GetDiscoverParseHistoryQuery(3, 2, marker, null, null), CancellationToken.None);

		page1.TotalCount.ShouldBe(5);
		page1.Items.Count.ShouldBe(2);
		page3.Items.Count.ShouldBe(1);
		page1.Items.Select(x => x.Id).ShouldNotContain(page3.Items[0].Id);
	}

	private static DiscoveredChannel NewChannel(Action<DiscoveredChannel>? setup = null)
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"stat_{Guid.NewGuid():N}",
			Title = "Channel",
			PeerType = "channel",
			Status = DiscoveryStatus.Pending
		};
		setup?.Invoke(channel);
		return channel;
	}

	private static int CountOf(IEnumerable<DiscoverStatusCount> counts, DiscoverChannelStatus status) =>
		counts.Where(x => x.Status == status).Sum(x => x.Count);

	private static int CountOf(IEnumerable<DiscoverNamedCount> counts, string name) =>
		counts.Where(x => x.Name == name).Sum(x => x.Count);

	private static int Delta(
		IEnumerable<DiscoverParticipantsBucketCount> after,
		IEnumerable<DiscoverParticipantsBucketCount> before,
		DiscoverParticipantsBucket bucket
	) =>
		after.Where(x => x.Bucket == bucket).Sum(x => x.Count)
		- before.Where(x => x.Bucket == bucket).Sum(x => x.Count);
}

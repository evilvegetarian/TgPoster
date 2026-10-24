using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.Parse.GetParseChannelStats;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ParseChannelStatsStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ParseChannelStatsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetSourceStatusCountsAsync_ShouldCountOnlyUserSources()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		await CreateSourceAsync(schedule, ParsingStatus.Failed);
		await new ChannelParsingSettingBuilder(context).WithStatus(ParsingStatus.Waiting).CreateAsync();

		var result = await sut.GetSourceStatusCountsAsync(schedule.UserId, CancellationToken.None);

		result.Single(x => x.Status == ParseChannelSourceStatus.Waiting).Count.ShouldBe(2);
		result.Single(x => x.Status == ParseChannelSourceStatus.Failed).Count.ShouldBe(1);
		result.Sum(x => x.Count).ShouldBe(3);
	}

	[Fact]
	public async Task GetPostTotalsAsync_ShouldSplitParsedPostsByState()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var source = await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		CreateMessage(schedule, source.Id, MessageStatus.Register, isVerified: true);
		CreateMessage(schedule, source.Id, MessageStatus.Register, isVerified: false);
		CreateMessage(schedule, source.Id, MessageStatus.Send, isVerified: true);
		CreateMessage(schedule, source.Id, MessageStatus.Error, isVerified: true);
		CreateMessage(schedule, null, MessageStatus.Register, isVerified: true);
		var deleted = CreateMessage(schedule, source.Id, MessageStatus.Register, isVerified: true);
		context.Messages.Remove(deleted);
		await context.SaveChangesAsync();

		var result = await sut.GetPostTotalsAsync(schedule.UserId, CancellationToken.None);

		result.Total.ShouldBe(4);
		result.Planned.ShouldBe(1);
		result.AwaitingApproval.ShouldBe(1);
		result.Published.ShouldBe(1);
		result.Failed.ShouldBe(1);
		result.Canceled.ShouldBe(0);
	}

	[Fact]
	public async Task GetRunTotalsAsync_ShouldSumUserRunsSinceMoment()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var source = await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		var foreignSource = await new ChannelParsingSettingBuilder(context).CreateAsync();
		await CreateRunAsync(source, now.AddHours(-1), published: 4, skippedAsAds: 2, fetched: 10);
		await CreateRunAsync(source, now.AddHours(-2), ParseChannelRunStatus.TemporaryFailure);
		await CreateRunAsync(source, now.AddDays(-3), published: 100);
		await CreateRunAsync(foreignSource, now.AddHours(-1), published: 50);

		var result = await sut.GetRunTotalsAsync(schedule.UserId, now.AddDays(-1), CancellationToken.None);

		result.Runs.ShouldBe(2);
		result.FailedRuns.ShouldBe(1);
		result.Published.ShouldBe(4);
		result.SkippedAsAds.ShouldBe(2);
		result.FetchedMessages.ShouldBe(10);
	}

	[Fact]
	public async Task GetRunTotalsAsync_ShouldReturnZeros_WhenNoRuns()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync();

		var result = await sut.GetRunTotalsAsync(schedule.UserId, DateTimeOffset.UtcNow.AddDays(-1),
			CancellationToken.None);

		result.ShouldBe(ParseChannelRunTotals.Empty);
	}

	[Fact]
	public async Task GetRunsByDayAsync_ShouldGroupRunsByUtcDay()
	{
		var day = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-2), TimeSpan.Zero);
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var source = await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		await CreateRunAsync(source, day.AddHours(1), published: 2, skippedAsDuplicates: 5);
		await CreateRunAsync(source, day.AddHours(20), published: 3, skippedAsAds: 1);
		await CreateRunAsync(source, day.AddDays(1).AddHours(1), published: 7);

		var result = await sut.GetRunsByDayAsync(schedule.UserId, day, CancellationToken.None);

		var first = result.Single(x => x.Date == DateOnly.FromDateTime(day.UtcDateTime));
		first.Published.ShouldBe(5);
		first.Skipped.ShouldBe(6);
		result.Single(x => x.Date == DateOnly.FromDateTime(day.UtcDateTime).AddDays(1)).Published.ShouldBe(7);
	}

	[Fact]
	public async Task GetTopSourcesAsync_ShouldOrderByPublishedAndShowLastRun()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var quiet = await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		var productive = await CreateSourceAsync(schedule, ParsingStatus.Failed);
		await CreateRunAsync(quiet, now.AddHours(-3), published: 1);
		await CreateRunAsync(productive, now.AddHours(-5), published: 10, skippedAsAds: 4);
		await CreateRunAsync(productive, now.AddHours(-1), ParseChannelRunStatus.SourceUnavailable);
		await CreateRunAsync(productive, now.AddDays(-10), published: 1000);
		CreateMessage(schedule, productive.Id, MessageStatus.Register, isVerified: true);

		var result = await sut.GetTopSourcesAsync(schedule.UserId, now.AddDays(-1), 10, CancellationToken.None);

		result.Select(x => x.Id).ShouldBe([productive.Id, quiet.Id]);
		var top = result[0];
		top.Status.ShouldBe(ParseChannelSourceStatus.Failed);
		top.Runs.ShouldBe(2);
		top.FailedRuns.ShouldBe(1);
		top.Published.ShouldBe(10);
		top.Skipped.ShouldBe(4);
		top.LastRunStatus.ShouldBe(ParseChannelRunStatus.SourceUnavailable);
		top.ParsedMessagesCount.ShouldBe(1);
	}

	[Fact]
	public async Task GetRunsAsync_ShouldFilterAndHideForeignRuns()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var source = await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		var otherSource = await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		var foreignSource = await new ChannelParsingSettingBuilder(context).CreateAsync();
		var failed = await CreateRunAsync(source, now.AddHours(-1), ParseChannelRunStatus.Failed);
		await CreateRunAsync(source, now.AddHours(-2));
		await CreateRunAsync(otherSource, now.AddHours(-1), ParseChannelRunStatus.Failed);
		await CreateRunAsync(foreignSource, now.AddHours(-1), ParseChannelRunStatus.Failed);

		var filtered = await sut.GetRunsAsync(
			new ListParseChannelRunsQuery(1, 20, source.Id, ParseChannelRunStatus.Failed, now.AddDays(-1), now),
			schedule.UserId, CancellationToken.None);
		var foreign = await sut.GetRunsAsync(
			new ListParseChannelRunsQuery(1, 20, foreignSource.Id, null, null, null),
			schedule.UserId, CancellationToken.None);

		filtered.TotalCount.ShouldBe(1);
		filtered.Items.Single().Id.ShouldBe(failed.Id);
		filtered.Items.Single().Channel.ShouldBe(source.Channel);
		foreign.TotalCount.ShouldBe(0);
	}

	[Fact]
	public async Task GetRunsAsync_ShouldReturnNewestFirstWithPaging()
	{
		var now = DateTimeOffset.UtcNow;
		var schedule = await new ScheduleBuilder(context).CreateAsync();
		var source = await CreateSourceAsync(schedule, ParsingStatus.Waiting);
		var oldest = await CreateRunAsync(source, now.AddHours(-3));
		await CreateRunAsync(source, now.AddHours(-2));
		await CreateRunAsync(source, now.AddHours(-1));

		var result = await sut.GetRunsAsync(new ListParseChannelRunsQuery(2, 2, null, null, null, null),
			schedule.UserId, CancellationToken.None);

		result.TotalCount.ShouldBe(3);
		result.Items.Select(x => x.Id).ShouldBe([oldest.Id]);
	}

	private Task<ChannelParsingSetting> CreateSourceAsync(Schedule schedule, ParsingStatus status) =>
		new ChannelParsingSettingBuilder(context).WithSchedule(schedule).WithStatus(status).CreateAsync();

	private Message CreateMessage(Schedule schedule, Guid? sourceId, MessageStatus status, bool isVerified) =>
		new MessageBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithChannelParsingSettingId(sourceId)
			.WithStatus(status)
			.WithIsVerified(isVerified)
			.Create();

	private async Task<ChannelParsingRun> CreateRunAsync(
		ChannelParsingSetting source,
		DateTimeOffset startedAt,
		ParseChannelRunStatus status = ParseChannelRunStatus.Success,
		int published = 0,
		int skippedAsAds = 0,
		int skippedAsDuplicates = 0,
		int fetched = 0
	)
	{
		var run = new ChannelParsingRun
		{
			Id = Guid.NewGuid(),
			ChannelParsingSettingId = source.Id,
			StartedAt = startedAt,
			FinishedAt = startedAt.AddMinutes(1),
			Status = status,
			Published = published,
			SkippedAsAds = skippedAsAds,
			SkippedAsDuplicates = skippedAsDuplicates,
			FetchedMessages = fetched
		};
		context.ChannelParsingRuns.Add(run);
		await context.SaveChangesAsync();
		return run;
	}
}

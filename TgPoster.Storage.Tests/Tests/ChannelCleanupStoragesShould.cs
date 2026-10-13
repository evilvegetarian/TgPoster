using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.ChannelCleanup.StartChannelCleanup;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.ChannelCleanup;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ChannelCleanupStoragesShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();

	[Fact]
	public async Task GetUsedChatsAsync_ShouldCollectRepostParseCommentAndScheduleChats()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(session.UserId).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(-1005550001)
			.CreateAsync();
		var parse = new ChannelParsingSettingBuilder(context).Build();
		parse.TelegramSessionId = session.Id;
		parse.Channel = "@ParseChan";
		context.Add(parse);
		context.Add(new CommentRepostSettings
		{
			Id = Guid.NewGuid(),
			WatchedChannel = "watchedchan",
			WatchedChannelId = 7770001,
			DiscussionGroupId = 7770002,
			TelegramSessionId = session.Id,
			ScheduleId = schedule.Id
		});
		await context.SaveChangesAsync();
		var sut = new PreviewChannelCleanupStorage(context);

		var used = await sut.GetUsedChatsAsync(session.Id, CancellationToken.None);

		used.Contains(5550001, null).ShouldBeTrue();
		used.Contains(schedule.ChannelId, null).ShouldBeTrue();
		used.Contains(1, "parsechan").ShouldBeTrue();
		used.Contains(7770001, null).ShouldBeTrue();
		used.Contains(7770002, null).ShouldBeTrue();
		used.Contains(2, "watchedchan").ShouldBeTrue();
		used.Contains(3, "junkchan").ShouldBeFalse();
	}

	[Fact]
	public async Task GetUsedChatsAsync_ShouldIgnoreDestinationsOfOtherSessions()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var otherSettings = await new RepostSettingsBuilder(context).CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(otherSettings.Id)
			.WithChatIdentifier(8880001)
			.CreateAsync();
		var sut = new PreviewChannelCleanupStorage(context);

		var used = await sut.GetUsedChatsAsync(session.Id, CancellationToken.None);

		used.Contains(8880001, null).ShouldBeFalse();
	}

	[Fact]
	public async Task CreateJobAsync_ShouldStorePendingItemsInOrder_AndReportActiveJob()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var sut = new StartChannelCleanupStorage(context, new GuidFactory());

		var jobId = await sut.CreateJobAsync(
			session.Id,
			[
				new ChannelCleanupItemDraft(101, 1010, false, ChatType.Channel, "Первый", "first"),
				new ChannelCleanupItemDraft(102, 0, true, ChatType.Group, "Группа", null)
			],
			CancellationToken.None);
		var activeJobId = await sut.GetActiveJobIdAsync(session.Id, CancellationToken.None);

		activeJobId.ShouldBe(jobId);
		var items = await context.ChannelCleanupJobItems
			.AsNoTracking()
			.Where(x => x.ChannelCleanupJobId == jobId)
			.OrderBy(x => x.Order)
			.ToListAsync();
		items.Select(x => x.ChatId).ShouldBe([101, 102]);
		items.ShouldAllBe(x => x.Outcome == ChannelCleanupOutcome.Pending);
		items[1].IsBasicGroup.ShouldBeTrue();
	}

	[Fact]
	public async Task GetActiveJobIdAsync_ShouldIgnoreJobWithoutPendingItems()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		await CreateJobAsync(session.Id, ChannelCleanupStatus.InProgress, ChannelCleanupOutcome.Left);
		var sut = new StartChannelCleanupStorage(context, new GuidFactory());

		var activeJobId = await sut.GetActiveJobIdAsync(session.Id, CancellationToken.None);

		activeJobId.ShouldBeNull();
	}

	[Fact]
	public async Task MarkPendingItemsNotProcessedAsync_ShouldTouchOnlyPendingItems()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var job = await CreateJobAsync(session.Id, ChannelCleanupStatus.InProgress,
			ChannelCleanupOutcome.Pending, ChannelCleanupOutcome.Left);
		var sut = new CleanupChannelsStorage(context);

		await sut.MarkPendingItemsNotProcessedAsync(job.Id, "сессия не авторизована", CancellationToken.None);

		var items = await context.ChannelCleanupJobItems
			.AsNoTracking()
			.Where(x => x.ChannelCleanupJobId == job.Id)
			.OrderBy(x => x.Order)
			.ToListAsync();
		items[0].Outcome.ShouldBe(ChannelCleanupOutcome.NotProcessed);
		items[0].Error.ShouldBe("сессия не авторизована");
		items[1].Outcome.ShouldBe(ChannelCleanupOutcome.Left);
	}

	[Fact]
	public async Task GetResumableJobIdsAsync_ShouldReturnCooldownJob_OnlyAfterSessionCooldown()
	{
		var freeSession = await new TelegramSessionBuilder(context).CreateAsync();
		var restrictedSession = await new TelegramSessionBuilder(context).CreateAsync();
		restrictedSession.FloodWaitUntil = DateTimeOffset.UtcNow.AddMinutes(30);
		await context.SaveChangesAsync();
		var resumable = await CreateJobAsync(freeSession.Id, ChannelCleanupStatus.CooldownWait,
			ChannelCleanupOutcome.Pending);
		var restricted = await CreateJobAsync(restrictedSession.Id, ChannelCleanupStatus.CooldownWait,
			ChannelCleanupOutcome.Pending);
		var sut = new ResumeChannelCleanupJobsStorage(context);

		var result = await sut.GetResumableJobIdsAsync(CancellationToken.None);

		result.ShouldContain(resumable.Id);
		result.ShouldNotContain(restricted.Id);
	}

	[Fact]
	public async Task GetJobStateAsync_ShouldReturnJobOnlyToSessionOwner()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var job = await CreateJobAsync(session.Id, ChannelCleanupStatus.Completed, ChannelCleanupOutcome.Left);
		var sut = new GetChannelCleanupJobStorage(context);

		var own = await sut.GetJobStateAsync(job.Id, session.UserId, CancellationToken.None);
		var foreign = await sut.GetJobStateAsync(job.Id, Guid.NewGuid(), CancellationToken.None);

		own.ShouldNotBeNull();
		own.Items.Single().Outcome.ShouldBe(ChannelCleanupOutcome.Left);
		foreign.ShouldBeNull();
	}

	private async Task<ChannelCleanupJob> CreateJobAsync(
		Guid sessionId,
		ChannelCleanupStatus status,
		params ChannelCleanupOutcome[] outcomes
	)
	{
		var jobId = Guid.NewGuid();
		var job = new ChannelCleanupJob
		{
			Id = jobId,
			TelegramSessionId = sessionId,
			Status = status,
			Items = outcomes
				.Select((x, i) => new ChannelCleanupJobItem
				{
					Id = Guid.NewGuid(),
					ChannelCleanupJobId = jobId,
					ChatId = 1000 + i,
					AccessHash = 1,
					ChatType = ChatType.Channel,
					Title = $"Канал {i}",
					Order = i,
					Outcome = x
				})
				.ToList()
		};

		await context.ChannelCleanupJobs.AddAsync(job);
		await context.SaveChangesAsync();

		return job;
	}
}

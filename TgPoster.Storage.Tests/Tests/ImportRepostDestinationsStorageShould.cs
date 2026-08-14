using Bogus;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class ImportRepostDestinationsStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private static readonly Faker faker = FakerProvider.Instance;
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ImportRepostDestinationsStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task GetJobAsync_ShouldReturnSettingsAndSessionState()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithDefaultSettings(15, 120, 3, 40, 7)
			.CreateAsync();
		var job = await CreateJobAsync(settings.Id, autoJoin: false);

		var result = await sut.GetJobAsync(job.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.RepostSettingsId.ShouldBe(settings.Id);
		result.TelegramSessionId.ShouldBe(session.Id);
		result.SourceChannelId.ShouldBe(schedule.ChannelId);
		result.AutoJoin.ShouldBeFalse();
		result.Status.ShouldBe(RepostImportStatus.Pending);
		result.DefaultDelayMinSeconds.ShouldBe(15);
		result.DefaultDelayMaxSeconds.ShouldBe(120);
		result.DefaultRepostEveryNth.ShouldBe(3);
		result.DefaultSkipProbability.ShouldBe(40);
		result.DefaultMaxRepostsPerDay.ShouldBe(7);
	}

	[Fact]
	public async Task GetPendingItemsAsync_ShouldReturnOnlyPendingWithFreshDiscoverData()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var job = await CreateJobAsync(settings.Id);
		var pendingChannel = await CreateDiscoveredChannelAsync(canSendMessages: false);
		var doneChannel = await CreateDiscoveredChannelAsync();

		await CreateItemAsync(job.Id, pendingChannel.Id, 0, AddDestinationOutcome.Pending);
		await CreateItemAsync(job.Id, doneChannel.Id, 1, AddDestinationOutcome.Added);

		var result = await sut.GetPendingItemsAsync(job.Id, CancellationToken.None);

		result.Count.ShouldBe(1);
		result[0].DiscoveredChannelId.ShouldBe(pendingChannel.Id);
		result[0].Username.ShouldBe(pendingChannel.Username);
		result[0].CanSendMessages.ShouldBe(false);
	}

	[Fact]
	public async Task GetPendingItemsAsync_ShouldPreserveQueueOrder()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var job = await CreateJobAsync(settings.Id);
		var first = await CreateDiscoveredChannelAsync();
		var second = await CreateDiscoveredChannelAsync();

		await CreateItemAsync(job.Id, second.Id, 1, AddDestinationOutcome.Pending);
		await CreateItemAsync(job.Id, first.Id, 0, AddDestinationOutcome.Pending);

		var result = await sut.GetPendingItemsAsync(job.Id, CancellationToken.None);

		result.Select(x => x.DiscoveredChannelId).ShouldBe([first.Id, second.Id]);
	}

	[Fact]
	public async Task UpdateItemAsync_ShouldStoreOutcomeAndProcessedAt()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var job = await CreateJobAsync(settings.Id);
		var channel = await CreateDiscoveredChannelAsync();
		var item = await CreateItemAsync(job.Id, channel.Id, 0, AddDestinationOutcome.Pending);
		var destinationId = Guid.NewGuid();

		await sut.UpdateItemAsync(item.Id, AddDestinationOutcome.Added, destinationId, null, CancellationToken.None);

		var updated = await context.RepostImportJobItems.AsNoTracking().FirstAsync(x => x.Id == item.Id);
		updated.Outcome.ShouldBe(AddDestinationOutcome.Added);
		updated.RepostDestinationId.ShouldBe(destinationId);
		updated.ProcessedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task UpdateItemAsync_ShouldTruncateTooLongError()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var job = await CreateJobAsync(settings.Id);
		var channel = await CreateDiscoveredChannelAsync();
		var item = await CreateItemAsync(job.Id, channel.Id, 0, AddDestinationOutcome.Pending);

		await sut.UpdateItemAsync(item.Id, AddDestinationOutcome.NotResolved, null, new string('x', 5000),
			CancellationToken.None);

		var updated = await context.RepostImportJobItems.AsNoTracking().FirstAsync(x => x.Id == item.Id);
		updated.Error!.Length.ShouldBe(1000);
	}

	[Fact]
	public async Task SetJobStatusAsync_ShouldStampStartedAndCompleted()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var job = await CreateJobAsync(settings.Id);

		await sut.SetJobStatusAsync(job.Id, RepostImportStatus.InProgress, null, CancellationToken.None);
		await sut.SetJobStatusAsync(job.Id, RepostImportStatus.Completed, null, CancellationToken.None);

		var updated = await context.RepostImportJobs.AsNoTracking().FirstAsync(x => x.Id == job.Id);
		updated.Status.ShouldBe(RepostImportStatus.Completed);
		updated.StartedAt.ShouldNotBeNull();
		updated.CompletedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task SetSessionFloodWaitAsync_ShouldRememberCooldown()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var floodWaitUntil = DateTimeOffset.UtcNow.AddMinutes(30);

		await sut.SetSessionFloodWaitAsync(session.Id, floodWaitUntil, CancellationToken.None);

		var updated = await context.TelegramSessions.AsNoTracking().FirstAsync(x => x.Id == session.Id);
		updated.FloodWaitUntil.ShouldNotBeNull();
	}

	[Fact]
	public async Task UpdateDiscoveredChannelAsync_ShouldRefreshInfoAndPermissions()
	{
		var channel = await CreateDiscoveredChannelAsync();
		var telegramId = faker.Random.Long(1_000_000_000, 9_000_000_000);

		await sut.UpdateDiscoveredChannelAsync(
			channel.Id,
			telegramId,
			"Свежее название",
			channel.Username,
			ChatType.Channel,
			true,
			false,
			CancellationToken.None);

		var refreshed = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.AsNoTracking()
			.FirstAsync(x => x.Id == channel.Id);

		refreshed.TelegramId.ShouldBe(telegramId);
		refreshed.Title.ShouldBe("Свежее название");
		refreshed.PeerType.ShouldBe("channel");
		refreshed.CanSendMessages.ShouldBe(true);
		refreshed.CanSendMedia.ShouldBe(false);
	}

	[Fact]
	public async Task AddDestinationAsync_ShouldCreateActiveDestinationLinkedToDiscover()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var channel = await CreateDiscoveredChannelAsync();
		var chatId = faker.Random.Long(1_000_000_000, 9_000_000_000);

		var destinationId = await sut.AddDestinationAsync(
			settings.Id,
			chatId,
			"Целевой канал",
			"targetchan",
			1000,
			ChatType.Channel,
			channel.Id,
			15,
			120,
			3,
			40,
			7,
			CancellationToken.None);

		var destination = await context.RepostDestinations.FirstOrDefaultAsync(x => x.Id == destinationId);

		destination.ShouldNotBeNull();
		destination.RepostSettingsId.ShouldBe(settings.Id);
		destination.ChatId.ShouldBe(chatId);
		destination.IsActive.ShouldBeTrue();
		destination.ChatStatus.ShouldBe(ChatStatus.Active);
		destination.ChatType.ShouldBe(ChatType.Channel);
		destination.DiscoveredChannelId.ShouldBe(channel.Id);
		destination.MemberCount.ShouldBe(1000);
		destination.DelayMinSeconds.ShouldBe(15);
		destination.DelayMaxSeconds.ShouldBe(120);
		destination.RepostEveryNth.ShouldBe(3);
		destination.SkipProbability.ShouldBe(40);
		destination.MaxRepostsPerDay.ShouldBe(7);
	}

	private async Task<RepostImportJob> CreateJobAsync(Guid repostSettingsId, bool autoJoin = true)
	{
		var job = new RepostImportJob
		{
			Id = Guid.NewGuid(),
			RepostSettingsId = repostSettingsId,
			AutoJoin = autoJoin,
			Status = RepostImportStatus.Pending
		};

		await context.RepostImportJobs.AddAsync(job);
		await context.SaveChangesAsync();

		return job;
	}

	private async Task<RepostImportJobItem> CreateItemAsync(
		Guid jobId,
		Guid discoveredChannelId,
		int order,
		AddDestinationOutcome outcome
	)
	{
		var item = new RepostImportJobItem
		{
			Id = Guid.NewGuid(),
			RepostImportJobId = jobId,
			DiscoveredChannelId = discoveredChannelId,
			Title = faker.Company.CompanyName(),
			Order = order,
			Outcome = outcome
		};

		await context.RepostImportJobItems.AddAsync(item);
		await context.SaveChangesAsync();

		return item;
	}

	private async Task<DiscoveredChannel> CreateDiscoveredChannelAsync(bool? canSendMessages = null)
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			TelegramId = faker.Random.Long(1_000_000_000, 9_000_000_000),
			Username = "discovered" + faker.Random.Number(10000, 99999),
			Title = faker.Company.CompanyName(),
			InviteHash = faker.Random.AlphaNumeric(16),
			ParticipantsCount = faker.Random.Number(100, 10000),
			Status = DiscoveryStatus.Completed,
			CanSendMessages = canSendMessages
		};

		await context.DiscoveredChannels.AddAsync(channel);
		await context.SaveChangesAsync();

		return channel;
	}
}

using Bogus;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;
using TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

namespace TgPoster.Storage.Tests.Tests;

public sealed class AddDestinationsFromDiscoverStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private static readonly Faker faker = FakerProvider.Instance;
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly AddDestinationsFromDiscoverStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task GetSettingsDefaultsAsync_WithOwner_ShouldReturnDefaultsAndSourceChannel()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.WithDefaultSettings(15, 120, 3, 40, 7)
			.CreateAsync();

		var result = await sut.GetSettingsDefaultsAsync(settings.Id, user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.TelegramSessionId.ShouldBe(session.Id);
		result.SourceChannelId.ShouldBe(schedule.ChannelId);
		result.SessionFloodWaitUntil.ShouldBeNull();
	}

	[Fact]
	public async Task GetSettingsDefaultsAsync_WithRestrictedSession_ShouldReturnFloodWait()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		var floodWaitUntil = DateTimeOffset.UtcNow.AddMinutes(30);
		var tracked = await context.TelegramSessions.FirstAsync(x => x.Id == session.Id);
		tracked.FloodWaitUntil = floodWaitUntil;
		await context.SaveChangesAsync();

		var result = await sut.GetSettingsDefaultsAsync(settings.Id, user.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.SessionFloodWaitUntil.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetSettingsDefaultsAsync_WithForeignUser_ShouldReturnNull()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();

		var result = await sut.GetSettingsDefaultsAsync(settings.Id, Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task GetCandidatesAsync_ShouldReturnOnlyRequestedChannels()
	{
		var requested = await CreateDiscoveredChannelAsync();
		var other = await CreateDiscoveredChannelAsync();

		var result = await sut.GetCandidatesAsync([requested.Id], CancellationToken.None);

		result.Count.ShouldBe(1);
		result[0].Id.ShouldBe(requested.Id);
		result[0].TelegramId.ShouldBe(requested.TelegramId);
		result[0].Username.ShouldBe(requested.Username);
		result[0].InviteHash.ShouldBe(requested.InviteHash);
		result.ShouldNotContain(x => x.Id == other.Id);
	}

	[Fact]
	public async Task GetCandidatesAsync_ShouldReturnKnownSendPermissions()
	{
		var channel = await CreateDiscoveredChannelAsync(canSendMessages: false, canSendMedia: true);

		var result = await sut.GetCandidatesAsync([channel.Id], CancellationToken.None);

		result.Single().CanSendMessages.ShouldBe(false);
		result.Single().CanSendMedia.ShouldBe(true);
	}

	[Fact]
	public async Task GetCandidatesAsync_ShouldSkipBannedChannels()
	{
		var banned = await CreateDiscoveredChannelAsync(isBanned: true);

		var result = await sut.GetCandidatesAsync([banned.Id], CancellationToken.None);

		result.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetExistingChatIdsAsync_ShouldReturnChatIdsOfSettingsOnly()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(4242)
			.CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithChatIdentifier(9999)
			.CreateAsync();

		var result = await sut.GetExistingChatIdsAsync(settings.Id, CancellationToken.None);

		result.ShouldBe([4242]);
	}

	[Fact]
	public async Task CreateImportJobAsync_ShouldStoreJobWithAllItemsInOrder()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();

		var jobId = await sut.CreateImportJobAsync(
			settings.Id,
			false,
			[
				new ImportJobItemDto(first, "Первый", AddDestinationOutcome.Pending, null),
				new ImportJobItemDto(second, "Второй", AddDestinationOutcome.NoWritePermission, "Нельзя писать")
			],
			CancellationToken.None);

		var job = await context.RepostImportJobs
			.Include(x => x.Items)
			.AsNoTracking()
			.FirstAsync(x => x.Id == jobId);

		job.RepostSettingsId.ShouldBe(settings.Id);
		job.AutoJoin.ShouldBeFalse();
		job.Status.ShouldBe(RepostImportStatus.Pending);

		var items = job.Items.OrderBy(x => x.Order).ToList();
		items.Count.ShouldBe(2);
		items[0].DiscoveredChannelId.ShouldBe(first);
		items[0].Outcome.ShouldBe(AddDestinationOutcome.Pending);
		items[0].ProcessedAt.ShouldBeNull();
		items[1].DiscoveredChannelId.ShouldBe(second);
		items[1].Outcome.ShouldBe(AddDestinationOutcome.NoWritePermission);
		items[1].Error.ShouldBe("Нельзя писать");
		items[1].ProcessedAt.ShouldNotBeNull();
	}

	private async Task<DiscoveredChannel> CreateDiscoveredChannelAsync(
		bool isBanned = false,
		bool? canSendMessages = null,
		bool? canSendMedia = null
	)
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
			CanSendMessages = canSendMessages,
			CanSendMedia = canSendMedia,
			IsBanned = isBanned
		};

		await context.DiscoveredChannels.AddAsync(channel);
		await context.SaveChangesAsync();

		return channel;
	}
}

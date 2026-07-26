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
		result.DefaultDelayMinSeconds.ShouldBe(15);
		result.DefaultDelayMaxSeconds.ShouldBe(120);
		result.DefaultRepostEveryNth.ShouldBe(3);
		result.DefaultSkipProbability.ShouldBe(40);
		result.DefaultMaxRepostsPerDay.ShouldBe(7);
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

	private async Task<DiscoveredChannel> CreateDiscoveredChannelAsync(bool isBanned = false)
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
			IsBanned = isBanned
		};

		await context.DiscoveredChannels.AddAsync(channel);
		await context.SaveChangesAsync();

		return channel;
	}
}

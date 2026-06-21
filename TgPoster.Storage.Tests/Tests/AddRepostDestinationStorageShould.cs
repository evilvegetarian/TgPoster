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

public sealed class AddRepostDestinationStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private static readonly Faker faker = FakerProvider.Instance;
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly AddRepostDestinationStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task GetTelegramSessionIdAsync_WithExistingSettings_ShouldReturnSessionId()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		var result = await sut.GetTelegramSessionIdAsync(settings.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.ShouldBe(session.Id);
	}

	[Fact]
	public async Task GetTelegramSessionIdAsync_WithNonExistingSettings_ShouldReturnNull()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.GetTelegramSessionIdAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task DestinationExistsAsync_WithExistingDestination_ShouldReturnTrue()
	{
		var chatId = 12314124;
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(chatId)
			.CreateAsync();

		var result = await sut.DestinationExistsAsync(settings.Id, chatId, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task DestinationExistsAsync_WithNonExistingDestination_ShouldReturnFalse()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();

		var result = await sut.DestinationExistsAsync(settings.Id, 1565488, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task AddDestinationAsync_WithValidData_ShouldCreateDestinationLinkedToDiscover()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var chatIdentifier = 12141241;
		var discoveredId = await sut.UpsertDiscoveredChannelAsync(
			chatIdentifier, "Test Channel", "testchannel", 1000,
			ChatType.Channel, true, true, CancellationToken.None);

		var destinationId = await sut.AddDestinationAsync(
			settings.Id,
			chatIdentifier,
			"Test Channel",
			"testchannel",
			1000,
			ChatType.Channel,
			ChatStatus.Active,
			null,
			discoveredId,
			CancellationToken.None);

		var createdDestination = await context.RepostDestinations
			.FirstOrDefaultAsync(x => x.Id == destinationId);

		createdDestination.ShouldNotBeNull();
		createdDestination.ChatId.ShouldBe(chatIdentifier);
		createdDestination.IsActive.ShouldBeTrue();
		createdDestination.RepostSettingsId.ShouldBe(settings.Id);
		createdDestination.DiscoveredChannelId.ShouldBe(discoveredId);
	}

	[Fact]
	public async Task UpsertDiscoveredChannelAsync_WhenChannelNotInDiscover_ShouldCreateWithStatuses()
	{
		var chatId = faker.Random.Long(1_000_000_000, 9_000_000_000);
		var username = "newrepostchan" + faker.Random.Number(1000, 9999);

		var discoveredId = await sut.UpsertDiscoveredChannelAsync(
			chatId,
			"Brand New Channel",
			username,
			500,
			ChatType.Channel,
			true,
			false,
			CancellationToken.None);

		var discovered = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => x.Id == discoveredId);

		discovered.ShouldNotBeNull();
		discovered.TelegramId.ShouldBe(chatId);
		discovered.Username.ShouldBe(username);
		discovered.Title.ShouldBe("Brand New Channel");
		discovered.ParticipantsCount.ShouldBe(500);
		discovered.PeerType.ShouldBe("channel");
		discovered.TgUrl.ShouldBe($"https://t.me/{username}");
		discovered.CanSendMessages.ShouldBe(true);
		discovered.CanSendMedia.ShouldBe(false);
	}

	[Fact]
	public async Task UpsertDiscoveredChannelAsync_WhenChannelExists_ShouldUpdateInfoAndStatuses()
	{
		var chatId = faker.Random.Long(1_000_000_000, 9_000_000_000);
		var username = "existingchan" + faker.Random.Number(1000, 9999);

		var existing = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			TelegramId = chatId,
			Username = username,
			Title = "Old Title",
			ParticipantsCount = 10,
			Status = DiscoveryStatus.Completed
		};
		await context.DiscoveredChannels.AddAsync(existing);
		await context.SaveChangesAsync();

		var discoveredId = await sut.UpsertDiscoveredChannelAsync(
			chatId,
			"Fresh Title",
			username,
			999,
			ChatType.Channel,
			true,
			true,
			CancellationToken.None);

		discoveredId.ShouldBe(existing.Id);

		var matchingCount = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.CountAsync(x => x.TelegramId == chatId);
		matchingCount.ShouldBe(1);

		var refreshed = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.FirstAsync(x => x.Id == existing.Id);
		refreshed.Title.ShouldBe("Fresh Title");
		refreshed.ParticipantsCount.ShouldBe(999);
		refreshed.PeerType.ShouldBe("channel");
		refreshed.CanSendMessages.ShouldBe(true);
		refreshed.CanSendMedia.ShouldBe(true);
	}
}
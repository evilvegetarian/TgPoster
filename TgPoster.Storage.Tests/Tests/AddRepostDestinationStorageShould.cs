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
	public async Task AddDestinationAsync_WithValidData_ShouldCreateDestination()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var chatIdentifier = 12141241;

		var response = await sut.AddDestinationAsync(
			settings.Id,
			chatIdentifier,
			"Test Channel",
			"testchannel",
			1000,
			ChatType.Channel,
			ChatStatus.Active,
			null,
			CancellationToken.None);

		var createdDestination = await context.RepostDestinations
			.FirstOrDefaultAsync(x => x.Id == response.DestinationId);

		createdDestination.ShouldNotBeNull();
		createdDestination.ChatId.ShouldBe(chatIdentifier);
		createdDestination.IsActive.ShouldBeTrue();
		createdDestination.RepostSettingsId.ShouldBe(settings.Id);
		createdDestination.DiscoveredChannelId.ShouldBe(response.DiscoveredChannelId);
	}

	[Fact]
	public async Task AddDestinationAsync_WhenChannelNotInDiscover_ShouldCreateDiscoveredChannel()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var chatId = faker.Random.Long(1_000_000_000, 9_000_000_000);
		var username = "newrepostchan" + faker.Random.Number(1000, 9999);

		var response = await sut.AddDestinationAsync(
			settings.Id,
			chatId,
			"Brand New Channel",
			username,
			500,
			ChatType.Channel,
			ChatStatus.Active,
			null,
			CancellationToken.None);

		var discovered = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => x.Id == response.DiscoveredChannelId);

		discovered.ShouldNotBeNull();
		discovered.TelegramId.ShouldBe(chatId);
		discovered.Username.ShouldBe(username);
		discovered.Title.ShouldBe("Brand New Channel");
		discovered.ParticipantsCount.ShouldBe(500);
		discovered.PeerType.ShouldBe("channel");
		discovered.TgUrl.ShouldBe($"https://t.me/{username}");
	}

	[Fact]
	public async Task AddDestinationAsync_WhenChannelExistsInDiscover_ShouldLinkAndUpdate()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
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

		var response = await sut.AddDestinationAsync(
			settings.Id,
			chatId,
			"Fresh Title",
			username,
			999,
			ChatType.Channel,
			ChatStatus.Active,
			null,
			CancellationToken.None);

		response.DiscoveredChannelId.ShouldBe(existing.Id);

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
	}
}
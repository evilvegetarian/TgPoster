using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.DiscoverChannelLinks;
using TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DiscoverChannelLinksStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly DiscoverChannelLinksStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task UpsertAsync_WhenChannelIsNew_ShouldSavePeerTypeAndTelegramId()
	{
		var username = "newchannel";

		await sut.UpsertAsync(new DiscoveredPeerUpsert
		{
			Username = username,
			TgUrl = "https://t.me/newchannel",
			TelegramId = 987654321,
			PeerType = "channel",
			Title = "Test Channel",
			ParticipantsCount = 1500
		}, CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == username, CancellationToken.None);

		saved.PeerType.ShouldBe("channel");
		saved.TelegramId.ShouldBe(987654321);
		saved.TgUrl.ShouldBe("https://t.me/newchannel");
		saved.Title.ShouldBe("Test Channel");
		saved.ParticipantsCount.ShouldBe(1500);
	}

	[Fact]
	public async Task UpsertAsync_WhenChannelExists_ShouldUpdatePeerTypeAndTelegramId()
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = "existingchat",
			Status = DiscoveryStatus.Pending
		};
		context.DiscoveredChannels.Add(channel);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		await sut.UpsertAsync(new DiscoveredPeerUpsert
		{
			Username = channel.Username,
			TelegramId = 11223344,
			PeerType = "chat",
			Title = "Updated Chat",
			ParticipantsCount = 500
		}, CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == channel.Username, CancellationToken.None);

		saved.PeerType.ShouldBe("chat");
		saved.TelegramId.ShouldBe(11223344);
		saved.Title.ShouldBe("Updated Chat");
		saved.ParticipantsCount.ShouldBe(500);
	}

	[Fact]
	public async Task GetChannelsToProcessAsync_ShouldReturnPendingChannelsBeforeCompleted()
	{
		var completed = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = "completedchannel",
			Status = DiscoveryStatus.Completed,
			LastDiscoveredAt = DateTimeOffset.UtcNow.AddHours(-1)
		};
		var pending = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = "pendingchannel",
			Status = DiscoveryStatus.Pending
		};
		context.DiscoveredChannels.AddRange(completed, pending);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetChannelsToProcessAsync(CancellationToken.None);

		var pendingIndex = result.FindIndex(x => x.Username == pending.Username);
		var completedIndex = result.FindIndex(x => x.Username == completed.Username);
		pendingIndex.ShouldBeLessThan(completedIndex);
	}

	[Fact]
	public async Task UpsertAsync_WhenChannelIsNew_ShouldSetDiscoveredFromChannelId()
	{
		var sourceChannel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"source_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending
		};
		context.DiscoveredChannels.Add(sourceChannel);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var discoveredUsername = $"discovered_{Guid.NewGuid():N}";
		await sut.UpsertAsync(new DiscoveredPeerUpsert
		{
			Username = discoveredUsername,
			TgUrl = $"https://t.me/{discoveredUsername}",
			TelegramId = 111222333,
			PeerType = "channel",
			Title = "Discovered Channel",
			ParticipantsCount = 500,
			DiscoveredFromChannelId = sourceChannel.Id
		}, CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == discoveredUsername, CancellationToken.None);

		saved.DiscoveredFromChannelId.ShouldBe(sourceChannel.Id);
	}

	[Fact]
	public async Task UpsertAsync_WhenChannelExists_ShouldNotOverwriteDiscoveredFromChannelId()
	{
		var sourceChannel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"originalsource_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending
		};
		var discoveredChannel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"existingdiscovered_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending,
			DiscoveredFromChannelId = sourceChannel.Id
		};
		context.DiscoveredChannels.AddRange(sourceChannel, discoveredChannel);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var anotherSourceId = Guid.NewGuid();
		await sut.UpsertAsync(new DiscoveredPeerUpsert
		{
			Username = discoveredChannel.Username,
			TelegramId = 999888777,
			PeerType = "channel",
			Title = "Updated Title",
			ParticipantsCount = 1000,
			DiscoveredFromChannelId = anotherSourceId
		}, CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == discoveredChannel.Username, CancellationToken.None);

		saved.DiscoveredFromChannelId.ShouldBe(sourceChannel.Id);
		saved.Title.ShouldBe("Updated Title");
	}

	[Fact]
	public async Task UpsertAsync_WithMarkAsCompleted_ShouldSetCompletedStatusAndDiscoveredAt()
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = "sourcechannel",
			Status = DiscoveryStatus.Pending
		};
		context.DiscoveredChannels.Add(channel);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		await sut.UpsertAsync(new DiscoveredPeerUpsert
		{
			Username = channel.Username,
			LastParsedId = 500,
			TelegramId = 44556677,
			PeerType = "channel",
			Title = "Source Channel",
			ParticipantsCount = 10000,
			MarkAsCompleted = true
		}, CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == channel.Username, CancellationToken.None);

		saved.LastParsedId.ShouldBe(500);
		saved.TelegramId.ShouldBe(44556677);
		saved.PeerType.ShouldBe("channel");
		saved.Title.ShouldBe("Source Channel");
		saved.ParticipantsCount.ShouldBe(10000);
		saved.Status.ShouldBe(DiscoveryStatus.Completed);
		saved.LastDiscoveredAt.ShouldNotBeNull();
	}
}

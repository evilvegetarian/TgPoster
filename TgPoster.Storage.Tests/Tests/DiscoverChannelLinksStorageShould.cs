using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.DiscoverChannelLinks;

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

		await sut.UpsertAsync(
			username,
			"https://t.me/newchannel",
			lastParsedId: null,
			telegramId: 987654321,
			peerType: "channel",
			title: "Test Channel",
			participantsCount: 1500,
			ct: CancellationToken.None);

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

		await sut.UpsertAsync(
			channel.Username,
			tgUrl: null,
			lastParsedId: null,
			telegramId: 11223344,
			peerType: "chat",
			title: "Updated Chat",
			participantsCount: 500,
			ct: CancellationToken.None);

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

		await sut.UpsertAsync(
			channel.Username,
			tgUrl: null,
			lastParsedId: 500,
			telegramId: 44556677,
			peerType: "channel",
			title: "Source Channel",
			participantsCount: 10000,
			markAsCompleted: true,
			CancellationToken.None);

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

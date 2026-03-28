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
			CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == username, CancellationToken.None);

		saved.PeerType.ShouldBe("channel");
		saved.TelegramId.ShouldBe(987654321);
		saved.TgUrl.ShouldBe("https://t.me/newchannel");
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
			CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == channel.Username, CancellationToken.None);

		saved.PeerType.ShouldBe("chat");
		saved.TelegramId.ShouldBe(11223344);
	}

	[Fact]
	public async Task UpdateLastParsedIdAsync_ShouldSavePeerTypeAndSetCompletedStatus()
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

		await sut.UpdateLastParsedIdAsync(
			channel.Username,
			lastParsedId: 500,
			telegramId: 44556677,
			peerType: "channel",
			CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == channel.Username, CancellationToken.None);

		saved.LastParsedId.ShouldBe(500);
		saved.TelegramId.ShouldBe(44556677);
		saved.PeerType.ShouldBe("channel");
		saved.Status.ShouldBe(DiscoveryStatus.Completed);
	}
}

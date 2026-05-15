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

		var result = await sut.GetChannelsToProcessAsync(1,CancellationToken.None);

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

	[Fact]
	public async Task BulkUpsertAsync_WithEmptyBatch_ShouldNotThrow()
	{
		await sut.BulkUpsertAsync([], CancellationToken.None);
	}

	[Fact]
	public async Task BulkUpsertAsync_WhenAllNew_ShouldInsertAll()
	{
		var u1 = $"bulknew1_{Guid.NewGuid():N}";
		var u2 = $"bulknew2_{Guid.NewGuid():N}";
		var u3 = $"bulknew3_{Guid.NewGuid():N}";

		await sut.BulkUpsertAsync([
			new DiscoveredPeerUpsert { Username = u1, TelegramId = 1001, PeerType = "channel", Title = "T1" },
			new DiscoveredPeerUpsert { Username = u2, TelegramId = 1002, PeerType = "channel", Title = "T2" },
			new DiscoveredPeerUpsert { Username = u3, TelegramId = 1003, PeerType = "chat", Title = "T3" }
		], CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.Where(x => x.Username == u1 || x.Username == u2 || x.Username == u3)
			.ToListAsync(CancellationToken.None);

		saved.Count.ShouldBe(3);
		saved.ShouldAllBe(x => x.Status == DiscoveryStatus.Pending);
	}

	[Fact]
	public async Task BulkUpsertAsync_WhenSomeExist_ShouldUpdateExistingAndInsertNew()
	{
		var existingUsername = $"bulkexisting_{Guid.NewGuid():N}";
		var newUsername = $"bulknew_{Guid.NewGuid():N}";
		context.DiscoveredChannels.Add(new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = existingUsername,
			Title = "Old Title",
			ParticipantsCount = 100,
			Status = DiscoveryStatus.Pending
		});
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		await sut.BulkUpsertAsync([
			new DiscoveredPeerUpsert
			{
				Username = existingUsername,
				Title = "Updated Title",
				ParticipantsCount = 200,
				PeerType = "channel"
			},
			new DiscoveredPeerUpsert
			{
				Username = newUsername,
				TelegramId = 5005,
				Title = "Brand New",
				PeerType = "channel"
			}
		], CancellationToken.None);

		var updated = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == existingUsername, CancellationToken.None);
		var inserted = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == newUsername, CancellationToken.None);

		updated.Title.ShouldBe("Updated Title");
		updated.ParticipantsCount.ShouldBe(200);
		updated.PeerType.ShouldBe("channel");
		inserted.TelegramId.ShouldBe(5005);
		inserted.Title.ShouldBe("Brand New");
	}

	[Fact]
	public async Task BulkUpsertAsync_WithDuplicateInBatch_ShouldMergeIntoSameRow()
	{
		var username = $"bulkdup_{Guid.NewGuid():N}";

		await sut.BulkUpsertAsync([
			new DiscoveredPeerUpsert
			{
				Username = username,
				Title = "First",
				ParticipantsCount = 100,
				PeerType = "channel"
			},
			new DiscoveredPeerUpsert
			{
				Username = username,
				Title = "Second",
				InviteHash = $"hash_{Guid.NewGuid():N}"
			}
		], CancellationToken.None);

		var rows = await context.DiscoveredChannels
			.Where(x => x.Username == username)
			.ToListAsync(CancellationToken.None);

		rows.Count.ShouldBe(1);
		rows[0].Title.ShouldBe("Second");
		rows[0].ParticipantsCount.ShouldBe(100);
		rows[0].InviteHash.ShouldNotBeNull();
	}

	[Fact]
	public async Task BulkUpsertAsync_MatchByTelegramId_ShouldFindExistingWithoutUsername()
	{
		var telegramId = 7007L + Random.Shared.NextInt64(1, 1_000_000);
		var newUsername = $"bulkbyid_{Guid.NewGuid():N}";
		context.DiscoveredChannels.Add(new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			TelegramId = telegramId,
			Title = "Private Original",
			Status = DiscoveryStatus.Pending
		});
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		await sut.BulkUpsertAsync([
			new DiscoveredPeerUpsert
			{
				Username = newUsername,
				TelegramId = telegramId,
				Title = "Resolved Public",
				PeerType = "channel"
			}
		], CancellationToken.None);

		var rows = await context.DiscoveredChannels
			.Where(x => x.TelegramId == telegramId)
			.ToListAsync(CancellationToken.None);

		rows.Count.ShouldBe(1);
		rows[0].Username.ShouldBe(newUsername);
		rows[0].Title.ShouldBe("Resolved Public");
	}

	[Fact]
	public async Task BulkUpsertAsync_ShouldNotOverwriteDiscoveredFromChannelId()
	{
		var sourceId = Guid.NewGuid();
		var existingUsername = $"bulkfrom_{Guid.NewGuid():N}";
		context.DiscoveredChannels.Add(new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = existingUsername,
			DiscoveredFromChannelId = sourceId,
			Status = DiscoveryStatus.Pending
		});
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		await sut.BulkUpsertAsync([
			new DiscoveredPeerUpsert
			{
				Username = existingUsername,
				Title = "Updated",
				DiscoveredFromChannelId = Guid.NewGuid()
			}
		], CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == existingUsername, CancellationToken.None);

		saved.DiscoveredFromChannelId.ShouldBe(sourceId);
		saved.Title.ShouldBe("Updated");
	}

	[Fact]
	public async Task MarkAsSkippedAsync_ShouldSetStatusAndTimestamp()
	{
		var entity = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"skip_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending
		};
		context.DiscoveredChannels.Add(entity);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		await sut.MarkAsSkippedAsync(entity.Id, CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Id == entity.Id, CancellationToken.None);

		saved.Status.ShouldBe(DiscoveryStatus.Skipped);
		saved.LastDiscoveredAt.ShouldNotBeNull();
	}
}

using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages.UpdateChannelStats;

namespace TgPoster.Storage.Tests.Tests;

public sealed class UpdateChannelStatsStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly UpdateChannelStatsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetChannelsToUpdateAsync_ShouldReturnOnlyChannelsWithUsername()
	{
		var withUsername = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"stats_u_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending
		};
		var withoutUsername = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			TelegramId = Random.Shared.NextInt64(),
			Status = DiscoveryStatus.Pending
		};
		context.DiscoveredChannels.AddRange(withUsername, withoutUsername);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetChannelsToUpdateAsync(100, CancellationToken.None);

		result.ShouldContain(x => x.Id == withUsername.Id);
		result.ShouldNotContain(x => x.Id == withoutUsername.Id);
	}

	[Fact]
	public async Task GetChannelsToUpdateAsync_ShouldNotReturnBannedChannels()
	{
		var banned = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"stats_banned_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending,
			IsBanned = true
		};
		context.DiscoveredChannels.Add(banned);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetChannelsToUpdateAsync(100, CancellationToken.None);

		result.ShouldNotContain(x => x.Id == banned.Id);
	}

	[Fact]
	public async Task GetChannelsToUpdateAsync_ShouldReturnNullUpdatedAtFirst()
	{
		var neverUpdated = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"stats_never_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending,
			ParticipantsUpdatedAt = null
		};
		var recentlyUpdated = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"stats_recent_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending,
			ParticipantsUpdatedAt = DateTimeOffset.UtcNow
		};
		context.DiscoveredChannels.AddRange(neverUpdated, recentlyUpdated);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetChannelsToUpdateAsync(100, CancellationToken.None);

		var neverIndex = result.FindIndex(x => x.Id == neverUpdated.Id);
		var recentIndex = result.FindIndex(x => x.Id == recentlyUpdated.Id);
		neverIndex.ShouldBeLessThan(recentIndex);
	}

	[Fact]
	public async Task UpdateParticipantsCountAsync_ShouldUpdateCountAndTimestamp()
	{
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"stats_upd_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Pending,
			ParticipantsCount = 100
		};
		context.DiscoveredChannels.Add(channel);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		await sut.UpdateParticipantsCountAsync(channel.Id, 9999, CancellationToken.None);

		var saved = await context.DiscoveredChannels
			.FirstAsync(x => x.Id == channel.Id, CancellationToken.None);

		saved.ParticipantsCount.ShouldBe(9999);
		saved.ParticipantsUpdatedAt.ShouldNotBeNull();
	}
}

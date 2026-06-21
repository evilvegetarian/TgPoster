using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;
using SortDirection = TgPoster.API.Domain.UseCases.Discover.ListDiscover.SortDirection;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DiscoverStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly DiscoverStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetDiscoverChannelsAsync_WhenNoCategoryFilter_ShouldReturnOnlyCompletedChannels()
	{
		var completed = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"completed_{Guid.NewGuid():N}",
			Title = "Completed Channel",
			Status = DiscoveryStatus.Completed,
			ParticipantsCount = 1000
		};
		var pending = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"pending_{Guid.NewGuid():N}",
			Title = "Pending Channel",
			Status = DiscoveryStatus.Pending
		};
		context.DiscoveredChannels.AddRange(completed, pending);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var query = new ListDiscoverQuery(1, 50, null, null, null, null, null, DiscoverSortBy.Participants,
			SortDirection.Desc);
		var result = await sut.GetDiscoverChannelsAsync(query, CancellationToken.None);

		result.Items.ShouldAllBe(x => x.Id != pending.Id);
		result.Items.ShouldContain(x => x.Id == completed.Id);
	}

	[Fact]
	public async Task GetDiscoverChannelsAsync_WhenCategoryFilter_ShouldReturnOnlyMatchingCategory()
	{
		var category = $"tech_{Guid.NewGuid():N}";
		var matching = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"tech_{Guid.NewGuid():N}",
			Title = "Tech Channel",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 500
		};
		var other = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"music_{Guid.NewGuid():N}",
			Title = "Music Channel",
			Status = DiscoveryStatus.Completed,
			Category = "music",
			ParticipantsCount = 200
		};
		context.DiscoveredChannels.AddRange(matching, other);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var query = new ListDiscoverQuery(1, 50, category, null, null, null, null, DiscoverSortBy.Participants,
			SortDirection.Desc);
		var result = await sut.GetDiscoverChannelsAsync(query, CancellationToken.None);

		result.Items.ShouldAllBe(x => x.Category == category);
		result.Items.ShouldContain(x => x.Id == matching.Id);
		result.Items.ShouldNotContain(x => x.Id == other.Id);
	}

	[Fact]
	public async Task GetDiscoverChannelsAsync_WhenSearchFilter_ShouldFilterByTitleOrUsername()
	{
		var suffix = Guid.NewGuid().ToString("N")[..8];
		var byTitle = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"sometitle_{suffix}",
			Title = $"Crypto News {suffix}",
			Status = DiscoveryStatus.Completed,
			ParticipantsCount = 300
		};
		var byUsername = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"cryptoworld_{suffix}",
			Title = "World Channel",
			Status = DiscoveryStatus.Completed,
			ParticipantsCount = 100
		};
		var unrelated = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"unrelated_{suffix}",
			Title = "Something Else",
			Status = DiscoveryStatus.Completed,
			ParticipantsCount = 50
		};
		context.DiscoveredChannels.AddRange(byTitle, byUsername, unrelated);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var query = new ListDiscoverQuery(1, 50, null, "crypto", null, null, null, DiscoverSortBy.Participants,
			SortDirection.Desc);
		var result = await sut.GetDiscoverChannelsAsync(query, CancellationToken.None);

		result.Items.ShouldContain(x => x.Id == byTitle.Id);
		result.Items.ShouldContain(x => x.Id == byUsername.Id);
	}

	[Fact]
	public async Task GetDiscoverChannelsAsync_WhenPeerTypeFilter_ShouldReturnOnlyMatchingType()
	{
		var marker = Guid.NewGuid().ToString("N")[..6];
		var channel = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"ch_{marker}",
			Title = $"Channel {marker}",
			Status = DiscoveryStatus.Completed,
			PeerType = "channel",
			ParticipantsCount = 100
		};
		var chat = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"chat_{marker}",
			Title = $"Chat {marker}",
			Status = DiscoveryStatus.Completed,
			PeerType = "chat",
			ParticipantsCount = 50
		};
		context.DiscoveredChannels.AddRange(channel, chat);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var query = new ListDiscoverQuery(1, 50, null, marker, "channel", null, null, DiscoverSortBy.Participants,
			SortDirection.Desc);
		var result = await sut.GetDiscoverChannelsAsync(query, CancellationToken.None);

		result.Items.ShouldContain(x => x.Id == channel.Id);
		result.Items.ShouldNotContain(x => x.Id == chat.Id);
	}

	[Fact]
	public async Task GetDiscoverChannelsAsync_ShouldSupportPagination()
	{
		var tag = Guid.NewGuid().ToString("N");
		for (var i = 0; i < 5; i++)
		{
			context.DiscoveredChannels.Add(new DiscoveredChannel
			{
				Id = Guid.NewGuid(),
				Username = $"page_ch_{tag}_{i}",
				Title = $"Channel {i}",
				Status = DiscoveryStatus.Completed,
				Category = tag,
				ParticipantsCount = i * 100
			});
		}

		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var page1 = await sut.GetDiscoverChannelsAsync(
			new ListDiscoverQuery(1, 2, tag, null, null, null, null, DiscoverSortBy.Participants, SortDirection.Desc),
			CancellationToken.None);
		var page2 = await sut.GetDiscoverChannelsAsync(
			new ListDiscoverQuery(2, 2, tag, null, null, null, null, DiscoverSortBy.Participants, SortDirection.Desc),
			CancellationToken.None);

		page1.TotalCount.ShouldBe(5);
		page1.Items.Count.ShouldBe(2);
		page2.Items.Count.ShouldBe(2);
		page1.Items.Select(x => x.Id).ShouldNotContain(y => page2.Items.Select(x => x.Id).Contains(y));
	}

	[Fact]
	public async Task GetDiscoverChannelsAsync_WhenMinParticipantsFilter_ShouldExcludeSmallerChannels()
	{
		var category = $"minflt_{Guid.NewGuid():N}";
		var big = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"big_{Guid.NewGuid():N}",
			Title = "Big Channel",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 5000
		};
		var small = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"small_{Guid.NewGuid():N}",
			Title = "Small Channel",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 100
		};
		context.DiscoveredChannels.AddRange(big, small);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var query = new ListDiscoverQuery(1, 50, category, null, null, 1000, null, DiscoverSortBy.Participants,
			SortDirection.Desc);
		var result = await sut.GetDiscoverChannelsAsync(query, CancellationToken.None);

		result.Items.ShouldContain(x => x.Id == big.Id);
		result.Items.ShouldNotContain(x => x.Id == small.Id);
	}

	[Fact]
	public async Task GetDiscoverChannelsAsync_WhenMinAndMaxParticipantsFilter_ShouldReturnOnlyInRange()
	{
		var category = $"rangeflt_{Guid.NewGuid():N}";
		var below = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"below_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 100
		};
		var inRange = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"inrange_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 5000
		};
		var above = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"above_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 50000
		};
		context.DiscoveredChannels.AddRange(below, inRange, above);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var query = new ListDiscoverQuery(1, 50, category, null, null, 1000, 10000, DiscoverSortBy.Participants,
			SortDirection.Desc);
		var result = await sut.GetDiscoverChannelsAsync(query, CancellationToken.None);

		result.Items.ShouldContain(x => x.Id == inRange.Id);
		result.Items.ShouldNotContain(x => x.Id == below.Id);
		result.Items.ShouldNotContain(x => x.Id == above.Id);
	}

	[Fact]
	public async Task GetDiscoverChannelsAsync_WhenSortByParticipantsAsc_ShouldOrderAscending()
	{
		var category = $"sortpart_{Guid.NewGuid():N}";
		var low = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"low_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 100
		};
		var high = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"high_{Guid.NewGuid():N}",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 9000
		};
		context.DiscoveredChannels.AddRange(high, low);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var query = new ListDiscoverQuery(1, 50, category, null, null, null, null, DiscoverSortBy.Participants,
			SortDirection.Asc);
		var result = await sut.GetDiscoverChannelsAsync(query, CancellationToken.None);

		var ids = result.Items.Select(x => x.Id).ToList();
		ids.IndexOf(low.Id).ShouldBeLessThan(ids.IndexOf(high.Id));
	}

	[Fact]
	public async Task GetDiscoverChannelsAsync_WhenSortByTitleAsc_ShouldOrderAlphabetically()
	{
		var category = $"sorttitle_{Guid.NewGuid():N}";
		var alpha = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"a_{Guid.NewGuid():N}",
			Title = "Aaa Channel",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 10
		};
		var beta = new DiscoveredChannel
		{
			Id = Guid.NewGuid(),
			Username = $"z_{Guid.NewGuid():N}",
			Title = "Zzz Channel",
			Status = DiscoveryStatus.Completed,
			Category = category,
			ParticipantsCount = 9000
		};
		context.DiscoveredChannels.AddRange(beta, alpha);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var query = new ListDiscoverQuery(1, 50, category, null, null, null, null, DiscoverSortBy.Title,
			SortDirection.Asc);
		var result = await sut.GetDiscoverChannelsAsync(query, CancellationToken.None);

		var ids = result.Items.Select(x => x.Id).ToList();
		ids.IndexOf(alpha.Id).ShouldBeLessThan(ids.IndexOf(beta.Id));
	}

	[Fact]
	public async Task GetCategoriesAsync_ShouldReturnDistinctSortedCategories()
	{
		var prefix = Guid.NewGuid().ToString("N")[..6];
		var channels = new[]
		{
			new DiscoveredChannel
			{
				Id = Guid.NewGuid(), Username = $"a_{prefix}_1", Status = DiscoveryStatus.Completed,
				Category = $"{prefix}_tech"
			},
			new DiscoveredChannel
			{
				Id = Guid.NewGuid(), Username = $"a_{prefix}_2", Status = DiscoveryStatus.Completed,
				Category = $"{prefix}_tech"
			},
			new DiscoveredChannel
			{
				Id = Guid.NewGuid(), Username = $"a_{prefix}_3", Status = DiscoveryStatus.Completed,
				Category = $"{prefix}_music"
			},
			new DiscoveredChannel
			{
				Id = Guid.NewGuid(), Username = $"a_{prefix}_4", Status = DiscoveryStatus.Pending,
				Category = $"{prefix}_pending"
			}
		};
		context.DiscoveredChannels.AddRange(channels);
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		var result = await sut.GetCategoriesAsync(CancellationToken.None);

		result.ShouldContain($"{prefix}_tech");
		result.ShouldContain($"{prefix}_music");
		result.ShouldNotContain($"{prefix}_pending");
		result.Distinct().Count().ShouldBe(result.Count);
		result.SequenceEqual(result.OrderBy(x => x)).ShouldBeTrue();
	}
}
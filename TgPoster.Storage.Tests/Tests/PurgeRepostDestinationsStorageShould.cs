using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class PurgeRepostDestinationsStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly PurgeRepostDestinationsStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetPurgeableIdsAsync_ShouldIncludeRemovedAndSkipPurgedAndForeign()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync(ct);
		var live = await new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).CreateAsync(ct);
		var removed = new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).Build();
		removed.Deleted = DateTimeOffset.UtcNow;
		var purged = new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).Build();
		purged.Deleted = DateTimeOffset.UtcNow;
		purged.PurgedAt = DateTimeOffset.UtcNow;
		context.AddRange(removed, purged);
		await context.SaveChangesAsync(ct);
		var foreign = await new RepostDestinationBuilder(context).CreateAsync(ct);

		var result = await sut.GetPurgeableIdsAsync(settings.Id, [live.Id, removed.Id, purged.Id, foreign.Id], ct);

		result.ShouldBe([live.Id, removed.Id], ignoreOrder: true);
	}

	[Fact]
	public async Task PurgeAsync_ShouldDeleteLiveAndMarkBothPurged()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync(ct);
		var live = await new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).CreateAsync(ct);
		var removed = new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).Build();
		var deletedAt = DateTimeOffset.UtcNow.AddDays(-1);
		removed.Deleted = deletedAt;
		context.Add(removed);
		await context.SaveChangesAsync(ct);

		await sut.PurgeAsync([live.Id, removed.Id], ct);

		var rows = await context.RepostDestinations
			.IgnoreQueryFilters()
			.AsNoTracking()
			.Where(x => x.Id == live.Id || x.Id == removed.Id)
			.ToListAsync(ct);
		rows.ShouldAllBe(x => x.PurgedAt != null && x.Deleted != null && !x.IsActive);
		rows.Single(x => x.Id == removed.Id).Deleted!.Value.ShouldBe(deletedAt, TimeSpan.FromMilliseconds(1));
	}
}

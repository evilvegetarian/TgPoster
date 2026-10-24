using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class RestoreRepostDestinationStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly RestoreRepostDestinationStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task GetRemovedAsync_ShouldReturnOnlyRemovedNotPurged()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync(ct);
		var live = await new RepostDestinationBuilder(context).WithRepostSettingsId(settings.Id).CreateAsync(ct);
		var removed = await CreateRemovedAsync(settings.Id, purged: false);
		var purged = await CreateRemovedAsync(settings.Id, purged: true);

		var removedResult = await sut.GetRemovedAsync(settings.Id, removed.Id, ct);
		var liveResult = await sut.GetRemovedAsync(settings.Id, live.Id, ct);
		var purgedResult = await sut.GetRemovedAsync(settings.Id, purged.Id, ct);
		var foreignResult = await sut.GetRemovedAsync(Guid.NewGuid(), removed.Id, ct);

		removedResult.ShouldNotBeNull();
		removedResult.ChatId.ShouldBe(removed.ChatId);
		liveResult.ShouldBeNull();
		purgedResult.ShouldBeNull();
		foreignResult.ShouldBeNull();
	}

	[Fact]
	public async Task RestoreAsync_ShouldReturnDestinationDisabled()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync(ct);
		var removed = await CreateRemovedAsync(settings.Id, purged: false);

		await sut.RestoreAsync(removed.Id, ct);

		var restored = await context.RepostDestinations.AsNoTracking().SingleAsync(x => x.Id == removed.Id, ct);
		restored.IsActive.ShouldBeFalse();
		restored.Deleted.ShouldBeNull();
		(await sut.ChatInListAsync(settings.Id, removed.ChatId, ct)).ShouldBeTrue();
	}

	private async Task<RepostDestination> CreateRemovedAsync(Guid settingsId, bool purged)
	{
		var destination = new RepostDestinationBuilder(context).WithRepostSettingsId(settingsId).Build();
		destination.Deleted = DateTimeOffset.UtcNow;
		destination.PurgedAt = purged ? DateTimeOffset.UtcNow : null;
		context.Add(destination);
		await context.SaveChangesAsync(ct);
		return destination;
	}
}

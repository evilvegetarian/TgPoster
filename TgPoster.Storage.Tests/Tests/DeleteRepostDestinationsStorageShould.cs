using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DeleteRepostDestinationsStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly CancellationToken ct = CancellationToken.None;
	private readonly DeleteRepostDestinationsStorage sut;

	public DeleteRepostDestinationsStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new DeleteRepostDestinationsStorage(context);
	}

	[Fact]
	public async Task SettingsExistsAsync_ForOwner_ShouldReturnTrue()
	{
		var (settings, userId) = await CreateSettingsAsync();

		var result = await sut.SettingsExistsAsync(settings.Id, userId, ct);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task SettingsExistsAsync_ForAnotherUser_ShouldReturnFalse()
	{
		var (settings, _) = await CreateSettingsAsync();

		var result = await sut.SettingsExistsAsync(settings.Id, Guid.NewGuid(), ct);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task GetExistingIdsAsync_ShouldReturnOnlyDestinationsOfSettings()
	{
		var (settings, _) = await CreateSettingsAsync();
		var (otherSettings, _) = await CreateSettingsAsync();
		var own = await CreateDestinationAsync(settings.Id);
		var foreign = await CreateDestinationAsync(otherSettings.Id);

		var result = await sut.GetExistingIdsAsync(settings.Id, [own.Id, foreign.Id, Guid.NewGuid()], ct);

		result.ShouldHaveSingleItem().ShouldBe(own.Id);
	}

	[Fact]
	public async Task DeleteAsync_ShouldSoftDeleteOnlyListedDestinations()
	{
		var (settings, _) = await CreateSettingsAsync();
		var first = await CreateDestinationAsync(settings.Id);
		var second = await CreateDestinationAsync(settings.Id);
		var kept = await CreateDestinationAsync(settings.Id);

		await sut.DeleteAsync([first.Id, second.Id], ct);

		var destinations = await context.Set<RepostDestination>()
			.IgnoreQueryFilters()
			.AsNoTracking()
			.Where(x => x.RepostSettingsId == settings.Id)
			.ToListAsync(ct);

		destinations.Single(x => x.Id == first.Id).Deleted.ShouldNotBeNull();
		destinations.Single(x => x.Id == second.Id).Deleted.ShouldNotBeNull();
		destinations.Single(x => x.Id == kept.Id).Deleted.ShouldBeNull();
	}

	private async Task<(RepostSettings Settings, Guid UserId)> CreateSettingsAsync()
	{
		var schedule = await new ScheduleBuilder(context).CreateAsync(ct);
		var settings = await new RepostSettingsBuilder(context)
			.WithSchedule(schedule)
			.CreateAsync(ct);

		return (settings, schedule.UserId);
	}

	private Task<RepostDestination> CreateDestinationAsync(Guid settingsId) =>
		new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settingsId)
			.CreateAsync(ct);
}

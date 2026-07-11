using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class UpdateRepostSettingsStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly UpdateRepostSettingsStorage sut;

	public UpdateRepostSettingsStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new UpdateRepostSettingsStorage(context);
	}

	[Fact]
	public async Task SettingsExistsAsync_WithExistingSettingsOfUser_ShouldReturnTrue()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var schedule = await new ScheduleBuilder(context).WithUserId(user.Id).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithScheduleId(schedule.Id)
			.CreateAsync();

		var result = await sut.SettingsExistsAsync(settings.Id, user.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task SettingsExistsAsync_WithForeignUser_ShouldReturnFalse()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();

		var result = await sut.SettingsExistsAsync(settings.Id, Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task UpdateSettingsAsync_ShouldUpdateActivityAndDefaultSettings()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();

		await sut.UpdateSettingsAsync(settings.Id, false, 20, 90, 2, 25, 10, CancellationToken.None);

		var updated = await context.Set<RepostSettings>()
			.AsNoTracking()
			.FirstAsync(x => x.Id == settings.Id);
		updated.IsActive.ShouldBeFalse();
		updated.DefaultDelayMinSeconds.ShouldBe(20);
		updated.DefaultDelayMaxSeconds.ShouldBe(90);
		updated.DefaultRepostEveryNth.ShouldBe(2);
		updated.DefaultSkipProbability.ShouldBe(25);
		updated.DefaultMaxRepostsPerDay.ShouldBe(10);
	}

	[Fact]
	public async Task UpdateSettingsAsync_ShouldResetMaxRepostsPerDay_WhenNullPassed()
	{
		var settings = await new RepostSettingsBuilder(context)
			.WithDefaultSettings(1, 2, 1, 0, 5)
			.CreateAsync();

		await sut.UpdateSettingsAsync(settings.Id, true, 1, 2, 1, 0, null, CancellationToken.None);

		var updated = await context.Set<RepostSettings>()
			.AsNoTracking()
			.FirstAsync(x => x.Id == settings.Id);
		updated.DefaultMaxRepostsPerDay.ShouldBeNull();
	}
}

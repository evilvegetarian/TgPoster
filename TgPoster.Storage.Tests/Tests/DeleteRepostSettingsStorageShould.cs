using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DeleteRepostSettingsStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly DeleteRepostSettingsStorage sut;

	public DeleteRepostSettingsStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new(context);
	}

	[Fact]
	public async Task RepostSettingsExistsAsync_WithExistingSettings_ShouldReturnTrue()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();

		var result = await sut.RepostSettingsExistsAsync(settings.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task RepostSettingsExistsAsync_WithNonExistingSettings_ShouldReturnFalse()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.RepostSettingsExistsAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteRepostSettingsAsync_WithValidId_ShouldDeleteSettings()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.CreateAsync();

		await sut.DeleteRepostSettingsAsync(settings.Id, CancellationToken.None);

		var deletedSettings = await context.Set<Data.Entities.RepostSettings>()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => x.Id == settings.Id);

		deletedSettings.ShouldNotBeNull();
		deletedSettings.Deleted.ShouldNotBeNull();
	}
}

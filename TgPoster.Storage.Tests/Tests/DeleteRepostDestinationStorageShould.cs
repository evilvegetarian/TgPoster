using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class DeleteRepostDestinationStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly DeleteRepostDestinationStorage sut;

	public DeleteRepostDestinationStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new(context);
	}

	[Fact]
	public async Task DestinationExistsAsync_WithExistingDestination_ShouldReturnTrue()
	{
		var destination = await new RepostDestinationBuilder(context).CreateAsync();

		var result = await sut.DestinationExistsAsync(destination.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task DestinationExistsAsync_WithNonExistingDestination_ShouldReturnFalse()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.DestinationExistsAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteDestinationAsync_WithValidId_ShouldDeleteDestination()
	{
		var destination = await new RepostDestinationBuilder(context).CreateAsync();

		await sut.DeleteDestinationAsync(destination.Id, CancellationToken.None);

		var deletedDestination = await context.Set<Data.Entities.RepostDestination>()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => x.Id == destination.Id);

		deletedDestination.ShouldNotBeNull();
		deletedDestination.Deleted.ShouldNotBeNull();
	}
}
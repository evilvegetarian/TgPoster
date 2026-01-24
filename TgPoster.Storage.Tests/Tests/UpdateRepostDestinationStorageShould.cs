using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class UpdateRepostDestinationStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly UpdateRepostDestinationStorage sut = new(fixture.GetDbContext());

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
	public async Task UpdateDestinationAsync_WithValidData_ShouldUpdateIsActive()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithIsActive(true)
			.CreateAsync();

		await sut.UpdateDestinationAsync(destination.Id, false, CancellationToken.None);

		var updatedDestination = await context.Set<Data.Entities.RepostDestination>()
			.FirstOrDefaultAsync(x => x.Id == destination.Id);

		updatedDestination.ShouldNotBeNull();
		updatedDestination.IsActive.ShouldBeFalse();
	}

	[Fact]
	public async Task UpdateDestinationAsync_ToActive_ShouldUpdateIsActive()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithIsActive(false)
			.CreateAsync();

		await sut.UpdateDestinationAsync(destination.Id, true, CancellationToken.None);

		var updatedDestination = await context.Set<Data.Entities.RepostDestination>()
			.FirstOrDefaultAsync(x => x.Id == destination.Id);

		updatedDestination.ShouldNotBeNull();
		updatedDestination.IsActive.ShouldBeTrue();
	}
}

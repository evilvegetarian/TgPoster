using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class UpdateRepostDestinationStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly UpdateRepostDestinationStorage sut;

	public UpdateRepostDestinationStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new UpdateRepostDestinationStorage(context);
	}

	[Fact]
	public async Task GetDestinationAsync_WithExistingDestination_ShouldReturnSessionChatAndStatus()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithChatIdentifier(1375846471)
			.WithUsername("wc_world")
			.WithIsActive(false)
			.WithChatStatus(ChatStatus.Left)
			.CreateAsync();
		var sessionId = await context.RepostSettings
			.Where(x => x.Id == destination.RepostSettingsId)
			.Select(x => x.TelegramSessionId)
			.FirstAsync();

		var result = await sut.GetDestinationAsync(destination.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.TelegramSessionId.ShouldBe(sessionId);
		result.ChatId.ShouldBe(1375846471);
		result.Username.ShouldBe("wc_world");
		result.IsActive.ShouldBeFalse();
		result.ChatStatus.ShouldBe(ChatStatus.Left);
	}

	[Fact]
	public async Task GetDestinationAsync_WithNonExistingDestination_ShouldReturnNull()
	{
		var result = await sut.GetDestinationAsync(Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task UpdateDestinationAsync_WithValidData_ShouldUpdateIsActive()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithIsActive(true)
			.CreateAsync();

		await sut.UpdateDestinationAsync(destination.Id, false, ChatStatus.Active, 0, 0, 1, 0, null,
			CancellationToken.None);

		var updatedDestination = await context.RepostDestinations
			.FirstOrDefaultAsync(x => x.Id == destination.Id);

		updatedDestination.ShouldNotBeNull();
		updatedDestination.IsActive.ShouldBeFalse();
	}

	[Fact]
	public async Task UpdateDestinationAsync_ToActive_ShouldUpdateIsActiveAndChatStatus()
	{
		var destination = await new RepostDestinationBuilder(context)
			.WithIsActive(false)
			.WithChatStatus(ChatStatus.Left)
			.CreateAsync();

		await sut.UpdateDestinationAsync(destination.Id, true, ChatStatus.Active, 0, 0, 1, 0, null,
			CancellationToken.None);

		var updatedDestination = await context.RepostDestinations
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == destination.Id);

		updatedDestination.ShouldNotBeNull();
		updatedDestination.IsActive.ShouldBeTrue();
		updatedDestination.ChatStatus.ShouldBe(ChatStatus.Active);
	}
}

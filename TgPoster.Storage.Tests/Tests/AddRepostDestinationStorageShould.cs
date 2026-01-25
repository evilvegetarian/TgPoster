using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Storages.Repost;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class AddRepostDestinationStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly AddRepostDestinationStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task GetTelegramSessionIdAsync_WithExistingSettings_ShouldReturnSessionId()
	{
		var session = await new TelegramSessionBuilder(context).CreateAsync();
		var settings = await new RepostSettingsBuilder(context)
			.WithTelegramSessionId(session.Id)
			.CreateAsync();

		var result = await sut.GetTelegramSessionIdAsync(settings.Id, CancellationToken.None);

		result.ShouldNotBeNull();
		result.ShouldBe(session.Id);
	}

	[Fact]
	public async Task GetTelegramSessionIdAsync_WithNonExistingSettings_ShouldReturnNull()
	{
		var nonExistingId = Guid.NewGuid();

		var result = await sut.GetTelegramSessionIdAsync(nonExistingId, CancellationToken.None);

		result.ShouldBeNull();
	}

	[Fact]
	public async Task DestinationExistsAsync_WithExistingDestination_ShouldReturnTrue()
	{
		var chatId = 12314124;
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		await new RepostDestinationBuilder(context)
			.WithRepostSettingsId(settings.Id)
			.WithChatIdentifier(chatId)
			.CreateAsync();

		var result = await sut.DestinationExistsAsync(settings.Id, chatId, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task DestinationExistsAsync_WithNonExistingDestination_ShouldReturnFalse()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();

		var result = await sut.DestinationExistsAsync(settings.Id, 1565488, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task AddDestinationAsync_WithValidData_ShouldCreateDestination()
	{
		var settings = await new RepostSettingsBuilder(context).CreateAsync();
		var chatIdentifier = 12141241;

		var response = await sut.AddDestinationAsync(
			settings.Id,
			chatIdentifier,
			CancellationToken.None);
		
		var createdDestination = await context.Set<RepostDestination>()
			.FirstOrDefaultAsync(x => x.Id == response);

		createdDestination.ShouldNotBeNull();
		createdDestination.Id.ShouldBe(settings.Id);
		createdDestination.ChatId.ShouldBe(chatIdentifier);
		createdDestination.IsActive.ShouldBeTrue();
		createdDestination.RepostSettingsId.ShouldBe(settings.Id);
	}
}
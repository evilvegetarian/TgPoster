using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class ListTelegramBotStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly ListTelegramBotStorage sut = new(fixture.GetDbContext());

	[Fact]
	public async Task BelongsToUserAsync_WithOwnBot_ShouldReturnTrue()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var bot = await new TelegramBotBuilder(context).WithOwnerId(user.Id).CreateAsync();

		var result = await sut.BelongsToUserAsync(user.Id, bot.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task BelongsToUserAsync_WithBotOfAnotherUser_ShouldReturnFalse()
	{
		var owner = await new UserBuilder(context).CreateAsync();
		var stranger = await new UserBuilder(context).CreateAsync();
		var bot = await new TelegramBotBuilder(context).WithOwnerId(owner.Id).CreateAsync();

		var result = await sut.BelongsToUserAsync(stranger.Id, bot.Id, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task BelongsToUserAsync_WithUnknownBot_ShouldReturnFalse()
	{
		var user = await new UserBuilder(context).CreateAsync();

		var result = await sut.BelongsToUserAsync(user.Id, Guid.NewGuid(), CancellationToken.None);

		result.ShouldBeFalse();
	}
}

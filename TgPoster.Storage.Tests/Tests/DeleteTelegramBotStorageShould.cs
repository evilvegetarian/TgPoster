using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class DeleteTelegramBotStorageShould : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context;
	private readonly DeleteTelegramBotStorage sut;

	public DeleteTelegramBotStorageShould(StorageTestFixture fixture)
	{
		context = fixture.GetDbContext();
		sut = new(context);
	}

	[Fact]
	public async Task ExistsAsync_WithExistingTelegramBot_ShouldReturnTrue()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var telegramBot = await new TelegramBotBuilder(context).WithOwnerId(user.Id).CreateAsync();

		var result = await sut.ExistsAsync(telegramBot.Id, user.Id, CancellationToken.None);

		result.ShouldBeTrue();
	}

	[Fact]
	public async Task ExistsAsync_WithNonExistingTelegramBot_ShouldReturnFalse()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var nonExistingBotId = Guid.NewGuid();

		var result = await sut.ExistsAsync(nonExistingBotId, user.Id, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task ExistsAsync_WithWrongUserId_ShouldReturnFalse()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var telegramBot = await new TelegramBotBuilder(context).WithOwnerId(user.Id).CreateAsync();
		var wrongUserId = Guid.NewGuid();

		var result = await sut.ExistsAsync(telegramBot.Id, wrongUserId, CancellationToken.None);

		result.ShouldBeFalse();
	}

	[Fact]
	public async Task DeleteTelegramBotAsync_WithExistingBot_ShouldMarkAsDeleted()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var telegramBot = await new TelegramBotBuilder(context).WithOwnerId(user.Id).CreateAsync();

		await sut.DeleteTelegramBotAsync(telegramBot.Id, user.Id, CancellationToken.None);

		var deletedBot = await context.TelegramBots
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => x.Id == telegramBot.Id);

		deletedBot.ShouldNotBeNull();
		deletedBot.Deleted.ShouldNotBeNull();
	}

	[Fact]
	public async Task DeleteTelegramBotAsync_WithWrongUserId_ShouldNotDeleteBot()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var telegramBot = await new TelegramBotBuilder(context).WithOwnerId(user.Id).CreateAsync();
		var wrongUserId = Guid.NewGuid();

		await sut.DeleteTelegramBotAsync(telegramBot.Id, wrongUserId, CancellationToken.None);

		var bot = await context.TelegramBots
			.FirstOrDefaultAsync(x => x.Id == telegramBot.Id);

		bot.ShouldNotBeNull();
		bot.Deleted.ShouldBeNull();
	}
}
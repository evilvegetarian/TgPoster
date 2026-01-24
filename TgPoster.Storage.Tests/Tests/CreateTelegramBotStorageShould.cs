using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class CreateTelegramBotStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly CreateTelegramBotStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task CreateTelegramBotAsync_WithValidData_ShouldCreateBot()
	{
		var botName = "nameBot";
		var user = await new UserBuilder(context).CreateAsync();
		var botId = await sut.CreateTelegramBotAsync(
			"Api Token",
			long.MaxValue,
			user.Id,
			botName,
			CancellationToken.None);

		var bot = await context.TelegramBots.FindAsync(botId);
		botId.ShouldBe(bot!.Id);
		botName.ShouldBe(bot.Name);
	}

	[Fact]
	public async Task CreateTelegramBotAsync_WithDuplicate_ShouldCreateBot()
	{
		var telegramBot = await new TelegramBotBuilder(context).CreateAsync();
		var botId = await sut.CreateTelegramBotAsync(
			telegramBot.ApiTelegram,
			telegramBot.ChatId,
			telegramBot.OwnerId,
			telegramBot.Name,
			CancellationToken.None);

		var bot = await context.TelegramBots.FindAsync(botId);
		botId.ShouldBe(bot!.Id);
		telegramBot.Name.ShouldBe(bot.Name);
	}
}
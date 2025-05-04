using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests;

public class CreateTelegramBotStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly CreateTelegramBotStorage sut = new(fixture.GetDbContext(), new GuidFactory());

    [Fact]
    public async Task CreateTelegramBotAsync_WithValidData_ShouldCreateBot()
    {
        var botName = "nameBot";
        var user = await helper.CreateUserAsync();
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
}
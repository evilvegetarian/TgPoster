using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public class DeleteTelegramBotStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
    private readonly PosterContext context = fixture.GetDbContext();
    private readonly Helper helper = new(fixture.GetDbContext());
    private readonly DeleteTelegramBotStorage sut = new(fixture.GetDbContext());

    [Fact]
    public async Task ExistsAsync_WithExistingTelegramBot_ShouldReturnTrue()
    {
        var user = await helper.CreateUserAsync();
        var telegramBot = await helper.CreateTelegramBotAsync(user.Id);

        var result = await sut.ExistsAsync(telegramBot.Id, user.Id, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingTelegramBot_ShouldReturnFalse()
    {
        var user = await helper.CreateUserAsync();
        var nonExistingBotId = Guid.NewGuid();

        var result = await sut.ExistsAsync(nonExistingBotId, user.Id, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithWrongUserId_ShouldReturnFalse()
    {
        var user = await helper.CreateUserAsync();
        var telegramBot = await helper.CreateTelegramBotAsync(user.Id);
        var wrongUserId = Guid.NewGuid();

        var result = await sut.ExistsAsync(telegramBot.Id, wrongUserId, CancellationToken.None);

        result.ShouldBeFalse();
    }

    /*[Fact]
    public async Task DeleteTelegramBotAsync_WithExistingBot_ShouldMarkAsDeleted()
    {
        var user = await helper.CreateUserAsync();
        var telegramBot = await helper.CreateTelegramBotAsync(user.Id);

        await sut.DeleteTelegramBotAsync(telegramBot.Id, user.Id, CancellationToken.None);

        var deletedBot = await context.TelegramBots
            .FirstOrDefaultAsync(x => x.Id == telegramBot.Id);
        
        deletedBot.ShouldNotBeNull();
        deletedBot.Deleted.ShouldNotBeNull();
        deletedBot.Deleted.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }*/

    // [Fact]
    // public async Task DeleteTelegramBotAsync_WithNonExistingBot_ShouldNotThrow()
    // {
    //     var user = await helper.CreateUserAsync();
    //     var nonExistingBotId = Guid.NewGuid();
    //
    //     var exception = await Should.NotThrowAsync(async () =>
    //         await sut.DeleteTelegramBotAsync(nonExistingBotId, user.Id, CancellationToken.None));
    //
    //     exception.ShouldBeNull();
    // }

    [Fact]
    public async Task DeleteTelegramBotAsync_WithWrongUserId_ShouldNotDeleteBot()
    {
        var user = await helper.CreateUserAsync();
        var telegramBot = await helper.CreateTelegramBotAsync(user.Id);
        var wrongUserId = Guid.NewGuid();

        await sut.DeleteTelegramBotAsync(telegramBot.Id, wrongUserId, CancellationToken.None);

        var bot = await context.TelegramBots
            .FirstOrDefaultAsync(x => x.Id == telegramBot.Id);
        
        bot.ShouldNotBeNull();
        bot.Deleted.ShouldBeNull();
    }
}

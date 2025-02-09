using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class TelegramBotSeeder(PosterContext context) : BaseSeeder(context)
{
    public override async Task Seed()
    {
        if (await context.TelegramBots.AnyAsync())
            return;

        var bot = new TelegramBot
        {
            Id = GlobalConst.TelegramNotWorkedBotId,
            Name = "TelegramBot",
            ApiTelegram = "API Key",
            ChatId = long.MaxValue,
            OwnerId = GlobalConst.UserId
        };

        await context.TelegramBots.AddRangeAsync(bot);
    }
}
using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Endpoint.Tests.Seeder;

internal class TelegramBotSeeder(PosterContext context, string api) : BaseSeeder
{
    public override async Task Seed()
    {
        if (await context.TelegramBots.AnyAsync())
            return;

        var bot = new TelegramBot
        {
            Id = GlobalConst.TelegramBotId,
            Name = "TelegramBot",
            ApiTelegram = "API Key",
            ChatId = long.MaxValue,
            OwnerId = GlobalConst.UserId
        };
        var bot2 = new TelegramBot
        {
            Id = GlobalConst.Worked.TelegramBotId,
            Name = "TelegramBot2",
            ApiTelegram = api,
            ChatId = GlobalConst.Worked.ChatIdTg,
            OwnerId = GlobalConst.Worked.UserId
        };

        await context.TelegramBots.AddRangeAsync(bot, bot2);
    }
}
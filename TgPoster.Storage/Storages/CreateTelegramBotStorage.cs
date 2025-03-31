using TgPoster.API.Domain.UseCases.TelegramBots.CreateTelegramBot;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateTelegramBotStorage(PosterContext context, GuidFactory guidFactory) : ICreateTelegramBotStorage
{
    public async Task<Guid> CreateTelegramBotAsync(
        string apiToken,
        long chatId,
        Guid ownerId,
        string name,
        CancellationToken cancellationToken
    )
    {
        var bot = new TelegramBot
        {
            Id = guidFactory.New(),
            ApiTelegram = apiToken,
            ChatId = chatId,
            OwnerId = ownerId,
            Name = name
        };
        await context.TelegramBots.AddAsync(bot, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return bot.Id;
    }
}
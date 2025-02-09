using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.UseCases.TelegramBots.ListTelegramBot;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class ListTelegramBotStorage(PosterContext context) : IListTelegramBotStorage
{
    public Task<List<TelegramBotResponse>> GetTelegramBotListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return context.TelegramBots
            .Where(x => x.OwnerId == userId)
            .Select(x => new TelegramBotResponse
            {
                Id = x.Id,
                Name = x.Name
            }).ToListAsync(cancellationToken);
    }
}
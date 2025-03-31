using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Services;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class GetTelegramBotStorage(PosterContext context) : IGetTelegramBotStorage
{
    public Task<string?> GetApiToken(Guid scheduleId, Guid userId, CancellationToken cancellationToken)
    {
        return context.Schedules
            .Where(x => x.Id == scheduleId)
            .Where(x => x.UserId == userId)
            .Select(x => x.TelegramBot.ApiTelegram)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
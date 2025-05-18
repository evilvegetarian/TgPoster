using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Services;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal class GetTelegramBotStorage(PosterContext context) : IGetTelegramBotStorage
{
    public Task<string?> GetApiTokenAsync(Guid scheduleId, Guid userId, CancellationToken ct)
    {
        return context.Schedules
            .Where(x => x.Id == scheduleId)
            .Where(x => x.UserId == userId)
            .Select(x => x.TelegramBot.ApiTelegram)
            .FirstOrDefaultAsync(ct);
    }
}
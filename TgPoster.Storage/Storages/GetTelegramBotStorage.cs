using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;
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

    public Task<TelegramBotDto?> GetTelegramBotByScheduleIdAsync(Guid scheduleId, Guid userId, CancellationToken ct)
    {
        return context.Schedules
            .Where(x => x.UserId == userId && x.Id == scheduleId)
            .Select(x => new TelegramBotDto
            {
                ApiTelegram = x.TelegramBot.ApiTelegram,
                ChatId = x.TelegramBot.ChatId
            }).FirstOrDefaultAsync(ct);
    }

    public Task<TelegramBotDto?> GetTelegramBotByMessageIdAsync(Guid messageId, Guid userId, CancellationToken ct)
    {
        return context.Messages
            .Where(x => x.Schedule.UserId == userId && x.Id == messageId)
            .Select(x => new TelegramBotDto
            {
                ApiTelegram = x.Schedule.TelegramBot.ApiTelegram,
                ChatId = x.Schedule.TelegramBot.ChatId
            }).FirstOrDefaultAsync(ct);
    }
}
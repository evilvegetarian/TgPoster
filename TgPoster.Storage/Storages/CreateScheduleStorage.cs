using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateScheduleStorage(PosterContext context, GuidFactory guidFactory) : ICreateScheduleStorage
{
    public async Task<Guid> CreateScheduleAsync(
        string name,
        Guid userId,
        Guid telegramBot,
        long channelId,
        string userNameChat,
        CancellationToken ct
    )
    {
        var schedule = new Schedule
        {
            Id = guidFactory.New(),
            Name = name,
            UserId = userId,
            TelegramBotId = telegramBot,
            ChannelId = channelId,
            ChannelName = userNameChat,
            IsActive = true
        };
        await context.Schedules.AddAsync(schedule, ct);
        await context.SaveChangesAsync(ct);
        return schedule.Id;
    }

    public Task<string?> GetApiTokenAsync(Guid telegramBotId, Guid userId, CancellationToken ct)
    {
        return context.TelegramBots.Where(x => x.Id == telegramBotId)
            .Where(x => x.OwnerId == userId)
            .Select(x => x.ApiTelegram)
            .FirstOrDefaultAsync(ct);
    }
}
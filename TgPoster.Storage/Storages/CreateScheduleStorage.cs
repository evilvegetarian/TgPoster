using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateScheduleStorage(PosterContext context, GuidFactory guidFactory) : ICreateScheduleStorage
{
    public async Task<Guid> CreateSchedule(
        string name,
        Guid userId,
        Guid telegramBot,
        long channelId,
        CancellationToken cancellationToken
    )
    {
        var schedule = new Schedule
        {
            Id = guidFactory.New(),
            Name = name,
            UserId = userId,
            TelegramBotId = telegramBot,
            ChannelId = channelId
        };
        await context.Schedules.AddAsync(schedule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return schedule.Id;
    }

    public Task<string?> GetApiToken(Guid telegramBotId, Guid userId, CancellationToken cancellationToken)
    {
        return context.TelegramBots.Where(x => x.Id == telegramBotId)
            .Where(x => x.OwnerId == userId)
            .Select(x => x.ApiTelegram)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
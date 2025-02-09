using TgPoster.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal class CreateScheduleStorage(PosterContext context, GuidFactory guidFactory) : ICreateScheduleStorage
{
    public async Task<Guid> CreateSchedule(string name, Guid userId, Guid telegramBot, CancellationToken cancellationToken )
    {
        var schedule = new Schedule
        {
            Id = guidFactory.New(),
            Name = name,
            UserId = userId,
            TelegramBotId = telegramBot
        };
        await context.Schedules.AddAsync(schedule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return schedule.Id;
    }
}
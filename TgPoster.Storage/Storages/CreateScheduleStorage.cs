using TgPoster.Domain.UseCases.Schedules.CreateSchedule;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal class CreateScheduleStorage(PosterContext context, GuidFactory guidFactory):ICreateScheduleStorage
{
    public async Task<Guid> CreateSchedule(string name, Guid userId)
    {
        var schedule = new Schedule
        {
            Id = guidFactory.New(),
            Name = name,
            UserId = userId
        };
        await context.Schedules.AddAsync(schedule);
        await context.SaveChangesAsync();
        return schedule.Id;
    }
}
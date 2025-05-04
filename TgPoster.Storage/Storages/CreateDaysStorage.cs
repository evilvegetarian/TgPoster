using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Days.CreateDays;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateDaysStorage(PosterContext context, GuidFactory guidFactory) : ICreateDaysStorage
{
    public async Task<bool> ScheduleExistAsync(Guid scheduleId, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Schedules.AnyAsync(x =>
                x.Id == scheduleId && x.UserId == userId,
            cancellationToken);
    }

    public async Task CreateDaysAsync(List<CreateDayDto> days, CancellationToken cancellationToken)
    {
        var daysToAdd = days.Select(x => new Day
        {
            Id = guidFactory.New(),
            ScheduleId = x.ScheduleId,
            DayOfWeek = x.DayOfWeek,
            TimePostings = x.TimePostings
        }).ToList();
        await context.Days.AddRangeAsync(daysToAdd, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DayOfWeek>> GetDayOfWeekAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        return await context.Days
            .Where(x => x.ScheduleId == scheduleId)
            .Select(x => x.DayOfWeek)
            .ToListAsync(cancellationToken);
    }
}
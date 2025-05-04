using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Days.GetDays;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class GetDaysStorage(PosterContext context) : IGetDaysStorage
{
    public async Task<bool> ScheduleExistAsync(Guid scheduleId, Guid userId, CancellationToken cancellationToken)
    {
        return await context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, cancellationToken);
    }

    public async Task<List<GetDaysResponse>> GetDaysAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        return await context.Days.Where(x => x.ScheduleId == scheduleId)
            .Select(x => new GetDaysResponse
            {
                Id = x.Id,
                ScheduleId = x.ScheduleId,
                DayOfWeek = x.DayOfWeek,
                TimePostings = x.TimePostings
            }).ToListAsync(cancellationToken);
    }
}
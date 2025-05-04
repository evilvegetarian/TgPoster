using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.GetSchedule;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class GetScheduleStorage(PosterContext context) : IGetScheduleStorage
{
    public Task<ScheduleResponse?> GetScheduleAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return context.Schedules
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(x => new ScheduleResponse
            {
                Id = x.Id,
                Name = x.Name
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
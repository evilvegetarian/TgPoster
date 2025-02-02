using Microsoft.EntityFrameworkCore;
using TgPoster.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal class ListScheduleStorage(PosterContext context) : IListScheduleStorage
{
    public async Task<List<ScheduleResponse>> GetListScheduleAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.Schedules.Where(x => x.UserId == userId)
            .Select(x => new ScheduleResponse
            {
                Id = x.Id,
                Name = x.Name
            }).ToListAsync(cancellationToken);
    }
}
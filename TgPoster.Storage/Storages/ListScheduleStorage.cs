using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class ListScheduleStorage(PosterContext context) : IListScheduleStorage
{
    public Task<List<ScheduleResponse>> GetListScheduleAsync(Guid userId, CancellationToken cancellationToken)
        => context.Schedules.Where(x => x.UserId == userId)
            .Select(x => new ScheduleResponse
            {
                Id = x.Id,
                Name = x.Name
            }).ToListAsync(cancellationToken);
}
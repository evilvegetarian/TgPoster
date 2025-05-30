using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateTimeStorage(PosterContext context) : IUpdateTimeStorage
{
    public Task<bool> DayExistAsync(Guid id, CancellationToken ct)
    {
        return context.Days.AnyAsync(x => x.Id == id, ct);
    }

    public async Task UpdateTimeDayAsync(Guid id, List<TimeOnly> times, CancellationToken ct)
    {
        var entity = await context.Days.FirstOrDefaultAsync(x => x.Id == id, ct);
        entity!.TimePostings = times;
        await context.SaveChangesAsync(ct);
    }
}
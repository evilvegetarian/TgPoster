using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.DeleteSchedule;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class DeleteScheduleStorage(PosterContext context) : IDeleteScheduleStorage
{
    public async Task DeleteSchedule(Guid id)
    {
        var entity = await context.Schedules.FirstOrDefaultAsync(x => x.Id == id);
        context.Schedules.Remove(entity!);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ScheduleExist(Guid id, Guid userId)
    {
        return await context.Schedules.AnyAsync(x => x.UserId == userId && x.Id == id);
    }
}
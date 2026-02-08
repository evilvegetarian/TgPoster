using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.UpdateActiveSchedule;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateStatusScheduleStorage(PosterContext context) : IUpdateStatusScheduleStorage
{
	public Task<bool> ExistSchedule(Guid id, Guid userId, CancellationToken ct)
	{
		return context.Schedules.AnyAsync(s => s.Id == id && s.UserId == userId, ct);
	}

	public async Task UpdateStatus(Guid id, CancellationToken ct)
	{
		var schedule = await context.Schedules.FindAsync(id, ct);
		schedule!.IsActive = !schedule.IsActive;
		await context.SaveChangesAsync(ct);
	}
}
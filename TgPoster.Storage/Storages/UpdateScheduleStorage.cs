using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateScheduleStorage(PosterContext context) : IUpdateScheduleStorage
{
	public async Task UpdateScheduleAsync(Guid id, Guid userId, string? name, Guid? youTubeAccountId, CancellationToken ct)
	{
		var schedule = await context.Schedules
			.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

		if (schedule is null)
		{
			return;
		}

		if (name is not null)
			schedule.Name = name;

		schedule.YouTubeAccountId = youTubeAccountId;
		await context.SaveChangesAsync(ct);
	}
}
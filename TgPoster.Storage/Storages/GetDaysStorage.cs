using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Days.GetDays;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class GetDaysStorage(PosterContext context) : IGetDaysStorage
{
	public Task<bool> ScheduleExistAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ct);
	}

	public async Task<List<DayResponse>> GetDaysAsync(Guid scheduleId, CancellationToken ct)
	{
		var daysFromDb = await context.Days
			.Where(x => x.ScheduleId == scheduleId)
			.OrderBy(x => x.DayOfWeek)
			.ToListAsync(ct);

		var result = daysFromDb.Select(x => new DayResponse
			{
				Id = x.Id,
				ScheduleId = x.ScheduleId,
				DayOfWeek = x.DayOfWeek,
				TimePostings = x.TimePostings.OrderBy(t => t).ToList()
			})
			.ToList();

		return result;
	}
}
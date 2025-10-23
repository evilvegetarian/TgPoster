using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateTimeStorage(PosterContext context, GuidFactory guidFactory) : IUpdateTimeStorage
{
	public Task<Guid> DayIdAsync(Guid scheduleId, DayOfWeek dayOfWeek, CancellationToken ct)
	{
		return context.Days
			.Where(x => x.ScheduleId == scheduleId
			            && x.DayOfWeek == dayOfWeek)
			.Select(x => x.Id)
			.FirstOrDefaultAsync(ct);
	}

	public Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ct);
	}

	public async Task UpdateTimeDayAsync(Guid id, List<TimeOnly> times, CancellationToken ct)
	{
		var entity = await context.Days.FirstOrDefaultAsync(x => x.Id == id, ct);
		entity!.TimePostings = times;
		await context.SaveChangesAsync(ct);
	}

	public Task CreateDayAsync(DayOfWeek dayOfWeek, Guid scheduleId, List<TimeOnly> times, CancellationToken ct)
	{
		var day = new Day
		{
			Id = guidFactory.New(),
			DayOfWeek = dayOfWeek,
			TimePostings = times,
			ScheduleId = scheduleId
		};
		context.Days.AddAsync(day, ct);
		return context.SaveChangesAsync(ct);
	}
}
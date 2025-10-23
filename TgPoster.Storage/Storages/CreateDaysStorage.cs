using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Days.CreateDays;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal sealed class CreateDaysStorage(PosterContext context, GuidFactory guidFactory) : ICreateDaysStorage
{
	public async Task<bool> ScheduleExistAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		return await context.Schedules.AnyAsync(x =>
				x.Id == scheduleId && x.UserId == userId,
			ct);
	}

	public async Task CreateDaysAsync(List<CreateDayDto> days, CancellationToken ct)
	{
		var daysToAdd = days.Select(x => new Day
		{
			Id = guidFactory.New(),
			ScheduleId = x.ScheduleId,
			DayOfWeek = x.DayOfWeek,
			TimePostings = x.TimePostings
		}).ToList();
		await context.Days.AddRangeAsync(daysToAdd, ct);
		await context.SaveChangesAsync(ct);
	}

	public async Task<List<DayOfWeek>> GetDayOfWeekAsync(Guid scheduleId, CancellationToken ct)
	{
		return await context.Days
			.Where(x => x.ScheduleId == scheduleId)
			.Select(x => x.DayOfWeek)
			.ToListAsync(ct);
	}
}
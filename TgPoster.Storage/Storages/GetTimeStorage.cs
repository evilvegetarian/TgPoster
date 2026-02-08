using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.GetTime;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class GetTimeStorage(PosterContext context) : IGetTimeStorage
{
	public Task<DateTimeOffset> GetTime(Guid scheduleId, CancellationToken ct)
	{
		return context.Messages
			.Where(s => s.ScheduleId == scheduleId)
			.Where(x => x.TimePosting > DateTimeOffset.UtcNow)
			.Select(x => x.TimePosting)
			.OrderByDescending(x => x)
			.FirstOrDefaultAsync(ct);
	}

	public Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Days
			.Where(x => x.ScheduleId == scheduleId)
			.ToDictionaryAsync(x => x.DayOfWeek, x => x.TimePostings.ToList(), ct);
	}
}
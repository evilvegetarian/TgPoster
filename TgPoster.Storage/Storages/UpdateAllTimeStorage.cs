using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.UpdateAllTime;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class UpdateAllTimeStorage(PosterContext context) : IUpdateAllTimeStorage
{
	public Task<bool> ExistAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ct);
	}

	public Task<List<Guid>> GetMessagesAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Messages
			.Where(x => x.ScheduleId == scheduleId)
			.Where(x => x.TimePosting > DateTime.UtcNow)
			.Where(x => x.Status == MessageStatus.Register)
			.Select(x => x.Id)
			.Distinct()
			.ToListAsync(ct);
	}

	public Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Schedules
			.Where(x => x.Id == scheduleId)
			.SelectMany(x => x.Days)
			.ToDictionaryAsync(x => x.DayOfWeek, d => d.TimePostings.ToList(), ct);
	}

	public async Task UpdateTimeAsync(List<Guid> messageIds, List<DateTimeOffset> times, CancellationToken ct)
	{
		var entities = await context.Messages
			.Where(x => messageIds.Contains(x.Id))
			.OrderBy(x => x.TimePosting)
			.ToListAsync(ct);

		for (var i = 0; i < messageIds.Count; i++) entities[i].TimePosting = times[i];

		await context.SaveChangesAsync(ct);
	}
}
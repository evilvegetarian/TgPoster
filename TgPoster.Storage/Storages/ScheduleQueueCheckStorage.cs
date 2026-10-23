using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ScheduleQueueCheck;

namespace TgPoster.Storage.Storages;

internal sealed class ScheduleQueueCheckStorage(PosterContext context) : IScheduleQueueCheckStorage
{
	public async Task<List<ScheduleQueueInfo>> GetActiveSchedulesQueueAsync(DateTimeOffset now, CancellationToken ct)
	{
		var schedules = await context.Schedules
			.Where(s => s.IsActive)
			.Select(s => new
			{
				s.Id,
				TimePostings = s.Days.Select(d => d.TimePostings).ToList(),
				PlannedCount = s.Messages.Count(m =>
					(m.Status == MessageStatus.Register || m.Status == MessageStatus.InHandle)
					&& m.IsVerified
					&& m.TimePosting > now),
				LastPlannedAt = s.Messages
					.Where(m => (m.Status == MessageStatus.Register || m.Status == MessageStatus.InHandle)
					            && m.IsVerified
					            && m.TimePosting > now)
					.Max(m => (DateTimeOffset?)m.TimePosting),
				AwaitingApprovalCount = s.Messages.Count(m =>
					m.Status == MessageStatus.Register
					&& !m.IsVerified
					&& m.TimePosting > now)
			})
			.ToListAsync(ct);

		// Время публикации лежит json-колонкой, поэтому расписания без слотов отсеиваются уже в памяти
		return schedules
			.Where(s => s.TimePostings.Any(times => times.Count > 0))
			.Select(s => new ScheduleQueueInfo(s.Id, s.PlannedCount, s.LastPlannedAt, s.AwaitingApprovalCount))
			.ToList();
	}
}

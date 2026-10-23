using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.ApproveMessages;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class ApproveMessagesStorage(PosterContext context) : IApproveMessagesStorage
{
	public Task<List<ApproveMessageInfo>> GetMessagesAsync(List<Guid> ids, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.Where(x => ids.Contains(x.Id))
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new ApproveMessageInfo(
				x.Id,
				x.ScheduleId,
				x.IsVerified,
				x.Status == MessageStatus.Register,
				x.TimePosting))
			.ToListAsync(ct);
	}

	public Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Days
			.Where(x => x.ScheduleId == scheduleId)
			.ToDictionaryAsync(x => x.DayOfWeek, x => x.TimePostings.OrderBy(time => time).ToList(), ct);
	}

	public Task<List<DateTimeOffset>> GetOccupiedTimesAsync(Guid scheduleId, DateTimeOffset from, CancellationToken ct)
	{
		return context.Messages
			.Where(x => x.ScheduleId == scheduleId)
			.Where(x => x.TimePosting > from)
			.Where(x => x.Status == MessageStatus.Register || x.Status == MessageStatus.InHandle)
			.Select(x => x.TimePosting)
			.ToListAsync(ct);
	}

	public async Task ApproveMessage(
		List<Guid> messageIds,
		Guid userId,
		IReadOnlyDictionary<Guid, DateTimeOffset> timePostings,
		CancellationToken ct
	)
	{
		var messages = await context.Messages
			.Where(x => messageIds.Contains(x.Id))
			.Where(x => x.Schedule.UserId == userId)
			.ToListAsync(ct);

		foreach (var message in messages)
		{
			message.IsVerified = true;
			if (timePostings.TryGetValue(message.Id, out var timePosting))
			{
				message.TimePosting = timePosting;
			}
		}

		await context.SaveChangesAsync(ct);
	}
}

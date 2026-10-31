using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.RetryMessages;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class RetryMessagesStorage(PosterContext context) : IRetryMessagesStorage
{
	public Task<List<RetryMessageInfo>> GetMessagesAsync(List<Guid> ids, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.Where(x => ids.Contains(x.Id))
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new RetryMessageInfo(x.Id, x.ScheduleId, x.Status == MessageStatus.Error, x.TimePosting))
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
			.Where(x => x.Status == MessageStatus.Register || x.Status == MessageStatus.InHandle || x.Status == MessageStatus.Sending)
			.Select(x => x.TimePosting)
			.ToListAsync(ct);
	}

	public async Task RescheduleAsync(IReadOnlyCollection<RetriedMessage> items, CancellationToken ct)
	{
		var ids = items.Select(x => x.Id).ToList();
		var messages = await context.Messages
			.Where(x => ids.Contains(x.Id))
			.ToDictionaryAsync(x => x.Id, ct);

		foreach (var item in items)
		{
			var message = messages[item.Id];
			message.Status = MessageStatus.Register;
			message.TimePosting = item.TimePosting;
			message.SendError = null;
			message.TelegramMessageId = null;
		}

		await context.SaveChangesAsync(ct);
	}
}

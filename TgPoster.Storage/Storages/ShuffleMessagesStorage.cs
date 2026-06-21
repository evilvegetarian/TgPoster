using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.ShuffleMessages;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class ShuffleMessagesStorage(PosterContext context) : IShuffleMessagesStorage
{
	public Task<bool> ExistAsync(Guid scheduleId, Guid userId, CancellationToken ct)
	{
		return context.Schedules.AnyAsync(x => x.Id == scheduleId && x.UserId == userId, ct);
	}

	public Task<List<MessageSlot>> GetMessagesAsync(Guid scheduleId, CancellationToken ct)
	{
		return context.Messages
			.Where(x => x.ScheduleId == scheduleId)
			.Where(x => x.TimePosting > DateTime.UtcNow)
			.Where(x => x.Status == MessageStatus.Register)
			.OrderBy(x => x.TimePosting)
			.Select(x => new MessageSlot(x.Id, x.TimePosting))
			.ToListAsync(ct);
	}

	public async Task UpdateTimeAsync(List<Guid> messageIds, List<DateTimeOffset> times, CancellationToken ct)
	{
		var entities = await context.Messages
			.Where(x => messageIds.Contains(x.Id))
			.ToListAsync(ct);

		var byId = entities.ToDictionary(x => x.Id);
		for (var i = 0; i < messageIds.Count; i++)
		{
			byId[messageIds[i]].TimePosting = times[i];
		}

		await context.SaveChangesAsync(ct);
	}
}

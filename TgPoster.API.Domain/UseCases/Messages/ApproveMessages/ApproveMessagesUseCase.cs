using MediatR;
using Security.IdentityServices;
using Shared.Services;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.ApproveMessages;

internal class ApproveMessagesUseCase(
	IApproveMessagesStorage storage,
	IIdentityProvider identity,
	TimePostingService timePostingService
) : IRequestHandler<ApproveMessagesCommand>
{
	// Отправщик раз в минуту забирает посты со временем позже текущего, более близкое время он может пропустить
	private static readonly TimeSpan OverdueMargin = TimeSpan.FromMinutes(2);

	public async Task Handle(ApproveMessagesCommand request, CancellationToken ct)
	{
		var ids = request.MessageIds.Distinct().ToList();
		var userId = identity.Current.UserId;
		var messages = await storage.GetMessagesAsync(ids, userId, ct);

		var foundIds = messages.Select(x => x.Id).ToHashSet();
		foreach (var id in ids)
		{
			if (!foundIds.Contains(id))
			{
				throw new MessageNotFoundException(id);
			}
		}

		var now = DateTimeOffset.UtcNow;
		var threshold = now.Add(OverdueMargin);
		var overdue = messages
			.Where(x => !x.IsVerified && x.IsRegistered && x.TimePosting <= threshold)
			.ToList();
		var timePostings = await GetFreeSlotsAsync(overdue, now, threshold, ct);

		await storage.ApproveMessage(ids, userId, timePostings, ct);
	}

	private async Task<Dictionary<Guid, DateTimeOffset>> GetFreeSlotsAsync(
		List<ApproveMessageInfo> messages,
		DateTimeOffset now,
		DateTimeOffset threshold,
		CancellationToken ct
	)
	{
		var timePostings = new Dictionary<Guid, DateTimeOffset>();
		foreach (var schedule in messages.GroupBy(x => x.ScheduleId))
		{
			var scheduleMessages = schedule.OrderBy(x => x.TimePosting).ToList();
			var scheduleTime = await storage.GetScheduleTimeAsync(schedule.Key, ct);
			var occupied = await storage.GetOccupiedTimesAsync(schedule.Key, now, ct);

			var slots = timePostingService.GetFreeSlots(scheduleMessages.Count, scheduleTime, occupied, threshold);
			if (slots.Count < scheduleMessages.Count)
			{
				throw new NotFoundTimeException();
			}

			for (var i = 0; i < scheduleMessages.Count; i++)
			{
				timePostings[scheduleMessages[i].Id] = slots[i];
			}
		}

		return timePostings;
	}
}

using MediatR;
using Security.IdentityServices;
using Shared.Services;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.RetryMessages;

/// <summary>
/// Use case повтора отправки постов, упавших с ошибкой
/// </summary>
internal sealed class RetryMessagesUseCase(
	IRetryMessagesStorage storage,
	IIdentityProvider identity,
	TimePostingService timePostingService
) : IRequestHandler<RetryMessagesCommand, RetryMessagesResponse>
{
	public async Task<RetryMessagesResponse> Handle(RetryMessagesCommand request, CancellationToken ct)
	{
		var ids = request.Ids.Distinct().ToList();
		var messages = await storage.GetMessagesAsync(ids, identity.Current.UserId, ct);

		var foundIds = messages.Select(x => x.Id).ToHashSet();
		foreach (var id in ids)
		{
			if (!foundIds.Contains(id))
			{
				throw new MessageNotFoundException(id);
			}
		}

		var notFailed = messages.FirstOrDefault(x => !x.IsFailed);
		if (notFailed is not null)
		{
			throw new MessageNotFailedException(notFailed.Id);
		}

		var now = DateTimeOffset.UtcNow;
		var items = request.Mode == RetryMode.Now
			? messages
				.OrderBy(x => x.TimePosting)
				.Select((x, i) => new RetriedMessage { Id = x.Id, TimePosting = now.AddMinutes(i + 1) })
				.ToList()
			: await GetFreeSlotItemsAsync(messages, now, ct);

		await storage.RescheduleAsync(items, ct);

		return new RetryMessagesResponse { Items = items };
	}

	private async Task<List<RetriedMessage>> GetFreeSlotItemsAsync(
		List<RetryMessageInfo> messages,
		DateTimeOffset now,
		CancellationToken ct
	)
	{
		var items = new List<RetriedMessage>();
		foreach (var schedule in messages.GroupBy(x => x.ScheduleId))
		{
			var scheduleMessages = schedule.OrderBy(x => x.TimePosting).ToList();
			var scheduleTime = await storage.GetScheduleTimeAsync(schedule.Key, ct);
			var occupied = await storage.GetOccupiedTimesAsync(schedule.Key, now, ct);

			var slots = timePostingService.GetFreeSlots(scheduleMessages.Count, scheduleTime, occupied, now);
			if (slots.Count < scheduleMessages.Count)
			{
				throw new NotFoundTimeException();
			}

			items.AddRange(scheduleMessages.Select((x, i) =>
				new RetriedMessage { Id = x.Id, TimePosting = slots[i] }));
		}

		return items;
	}
}

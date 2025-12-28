using MediatR;
using Security.Interfaces;
using Shared;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Messages.UpdateAllTime;

public class UpdateAllTimeHandler(
	IUpdateAllTimeStorage storage,
	TimePostingService postingService,
	IIdentityProvider provider)
	: IRequestHandler<UpdateAllTimeCommand>
{
	public async Task Handle(UpdateAllTimeCommand request, CancellationToken ct)
	{
		if (!await storage.ExistAsync(request.ScheduleId, provider.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		var messages = await storage.GetMessagesAsync(request.ScheduleId, ct);
		if (!messages.Any())
		{
		}

		var scheduleTime = await storage.GetScheduleTimeAsync(request.ScheduleId, ct);

		var times = postingService.GetTimeForPosting(messages.Count, scheduleTime, DateTimeOffset.UtcNow);
		if (messages.Count != times.Count)
		{
			throw new Exception("время не равно количество. проблема");
		}

		await storage.UpdateTimeAsync(messages, times, ct);
	}
}
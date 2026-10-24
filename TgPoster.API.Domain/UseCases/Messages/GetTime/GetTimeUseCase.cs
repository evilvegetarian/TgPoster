using MediatR;
using Security.IdentityServices;
using Shared.Services;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.GetTime;

internal sealed class GetTimeUseCase(
	IGetTimeStorage storage,
	TimePostingService timePostingService,
	IIdentityProvider identity)
	: IRequestHandler<GetTimeCommand, GetTimeResponse>
{
	public async Task<GetTimeResponse> Handle(GetTimeCommand request, CancellationToken ct)
	{
		if (!await storage.ExistScheduleAsync(request.ScheduleId, identity.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		var time = await storage.GetTime(request.ScheduleId, ct);
		var scheduleTime = await storage.GetScheduleTimeAsync(request.ScheduleId, ct);

		var postingTime = timePostingService.GetTimeForPosting(100, scheduleTime, [time]);
		return new GetTimeResponse
		{
			PostingTimes = postingTime
		};
	}
}
using MediatR;
using Shared.Services;

namespace TgPoster.API.Domain.UseCases.Messages.GetTime;

internal sealed class GetTimeUseCase(IGetTimeStorage storage, TimePostingService timePostingService)
	: IRequestHandler<GetTimeCommand, GetTimeResponse>
{
	public async Task<GetTimeResponse> Handle(GetTimeCommand request, CancellationToken ct)
	{
		var time = await storage.GetTime(request.ScheduleId, ct);
		var scheduleTime = await storage.GetScheduleTimeAsync(request.ScheduleId, ct);

		var postingTime = timePostingService.GetTimeForPosting(100, scheduleTime, [time]);
		return new GetTimeResponse
		{
			PostingTimes = postingTime
		};
	}
}
using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

internal sealed class UpdateScheduleUseCase(IUpdateScheduleStorage storage, IIdentityProvider provider)
	: IRequestHandler<UpdateScheduleCommand>
{
	public async Task Handle(UpdateScheduleCommand request, CancellationToken ct)
	{
		await storage.UpdateScheduleAsync(request.Id, provider.Current.UserId, request.YouTubeAccountId, ct);
	}
}

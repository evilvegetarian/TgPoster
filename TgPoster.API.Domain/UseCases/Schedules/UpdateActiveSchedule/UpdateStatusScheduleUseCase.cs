using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Schedules.UpdateActiveSchedule;

internal class UpdateStatusScheduleUseCase(IUpdateStatusScheduleStorage storage, IIdentityProvider provider)
	: IRequestHandler<UpdateStatusScheduleCommand>
{
	public async Task Handle(UpdateStatusScheduleCommand request, CancellationToken cancellationToken)
	{
		var userId = provider.Current.UserId;
		if (!await storage.ExistSchedule(request.Id, userId, cancellationToken))
		{
			throw new ScheduleNotFoundException(request.Id);
		}

		await storage.UpdateStatus(request.Id, cancellationToken);
	}
}
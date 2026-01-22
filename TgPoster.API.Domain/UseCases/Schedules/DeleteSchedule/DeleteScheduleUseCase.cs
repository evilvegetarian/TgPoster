using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Schedules.DeleteSchedule;

internal sealed class DeleteScheduleUseCase(IDeleteScheduleStorage storage, IIdentityProvider identity)
	: IRequestHandler<DeleteScheduleCommand>
{
	public async Task Handle(DeleteScheduleCommand request, CancellationToken ct)
	{
		if (!await storage.ScheduleExistAsync(request.Id, identity.Current.UserId))
		{
			throw new ScheduleNotFoundException(request.Id);
		}

		await storage.DeleteScheduleAsync(request.Id);
	}
}
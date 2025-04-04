using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Schedules.DeleteSchedule;

internal sealed class DeleteScheduleUseCase(IDeleteScheduleStorage storage, IIdentityProvider identity)
    : IRequestHandler<DeleteScheduleCommand>
{
    public async Task Handle(DeleteScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!await storage.ScheduleExist(request.Id, identity.Current.UserId))
        {
            throw new ScheduleNotFoundException();
        }

        await storage.DeleteSchedule(request.Id);
    }
}
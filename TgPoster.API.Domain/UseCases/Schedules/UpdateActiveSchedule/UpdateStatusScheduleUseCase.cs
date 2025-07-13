using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Schedules.UpdateActiveSchedule;

internal class UpdateStatusScheduleUseCase(IUpdateStatusScheduleStorage storage)
    : IRequestHandler<UpdateStatusScheduleCommand>
{
    public async Task Handle(UpdateStatusScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!await storage.ExistSchedule(request.Id, cancellationToken))
        {
            throw new ScheduleNotFoundException();
        }

        await storage.UpdateStatus(request.Id, cancellationToken);
    }
}
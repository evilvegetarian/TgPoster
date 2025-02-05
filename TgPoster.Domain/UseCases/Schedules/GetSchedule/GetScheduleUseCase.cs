using Security;
using MediatR;
using TgPoster.Domain.Exceptions;
using TgPoster.Domain.UseCases.Schedules.ListSchedule;

namespace TgPoster.Domain.UseCases.Schedules.GetSchedule;

internal sealed class GetScheduleUseCase(IGetScheduleStorage storage, IIdentityProvider identity)
    : IRequestHandler<GetScheduleCommand, ScheduleResponse>
{
    public async Task<ScheduleResponse> Handle(GetScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await storage.GetSchedule(request.Id, identity.Current.UserId, cancellationToken);
        if (schedule is null)
        {
            throw new ScheduleNotFoundException();
        }

        return schedule;
    }
}
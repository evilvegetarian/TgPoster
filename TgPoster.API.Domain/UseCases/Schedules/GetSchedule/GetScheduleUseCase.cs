using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

namespace TgPoster.API.Domain.UseCases.Schedules.GetSchedule;

internal sealed class GetScheduleUseCase(IGetScheduleStorage storage, IIdentityProvider identity)
    : IRequestHandler<GetScheduleCommand, ScheduleResponse>
{
    public async Task<ScheduleResponse> Handle(GetScheduleCommand request, CancellationToken ct)
    {
        var schedule = await storage.GetScheduleAsync(request.Id, identity.Current.UserId, ct);
        if (schedule is null)
            throw new ScheduleNotFoundException(request.Id);

        return schedule;
    }
}
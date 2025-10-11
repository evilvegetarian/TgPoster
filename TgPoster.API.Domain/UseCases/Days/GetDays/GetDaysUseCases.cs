using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Days.GetDays;

internal sealed class GetDaysUseCases(IGetDaysStorage storage, IIdentityProvider identity)
    : IRequestHandler<GetDaysQuery, List<GetDaysResponse>>
{
    public async Task<List<GetDaysResponse>> Handle(GetDaysQuery request, CancellationToken ct)
    {
        var existDays = await storage.ScheduleExistAsync(request.ScheduleId, identity.Current.UserId, ct);
        if (!existDays)
        {
            throw new ScheduleNotFoundException(request.ScheduleId);
        }

        return await storage.GetDaysAsync(request.ScheduleId, ct);
    }
}
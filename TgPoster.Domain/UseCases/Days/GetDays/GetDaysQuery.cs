using Auth;
using MediatR;
using TgPoster.Domain.Exceptions;

namespace TgPoster.Domain.UseCases.Days.GetDays;

public sealed record GetDaysQuery(Guid ScheduleId) : IRequest<List<GetDaysResponse>>;

internal sealed class GetDaysUseCases(IGetDaysStorage storage, IIdentityProvider identity)
    : IRequestHandler<GetDaysQuery, List<GetDaysResponse>>
{
    public async Task<List<GetDaysResponse>> Handle(GetDaysQuery request, CancellationToken cancellationToken)
    {
        var existDays = await storage.ScheduleExist(request.ScheduleId, identity.Current.UserId, cancellationToken);
        if (!existDays)
        {
            throw new ScheduleNotFoundException();
        }

        return await storage.GetDays(request.ScheduleId, cancellationToken);
    }
}
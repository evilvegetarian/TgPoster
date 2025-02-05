using Security;
using MediatR;

namespace TgPoster.Domain.UseCases.Schedules.ListSchedule;

internal sealed class ListScheduleUseCases(IListScheduleStorage storage, IIdentityProvider identity)
    : IRequestHandler<ListScheduleQuery, List<ScheduleResponse>>
{
    public async Task<List<ScheduleResponse>> Handle(ListScheduleQuery request, CancellationToken cancellationToken)
    {
        var listSchedule = await storage.GetListScheduleAsync(identity.Current.UserId, cancellationToken);
        return listSchedule;
    }
}
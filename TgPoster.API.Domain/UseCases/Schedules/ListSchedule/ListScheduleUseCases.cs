using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

internal sealed class ListScheduleUseCases(IListScheduleStorage storage, IIdentityProvider identity)
    : IRequestHandler<ListScheduleQuery, List<ScheduleResponse>>
{
    public async Task<List<ScheduleResponse>> Handle(ListScheduleQuery request, CancellationToken ct)
    {
        var listSchedule = await storage.GetListScheduleAsync(identity.Current.UserId, ct);
        return listSchedule;
    }
}
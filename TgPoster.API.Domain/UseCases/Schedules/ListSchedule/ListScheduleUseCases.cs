using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

internal sealed class ListScheduleUseCases(IListScheduleStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListScheduleQuery, List<ScheduleResponse>>
{
	public Task<List<ScheduleResponse>> Handle(ListScheduleQuery request, CancellationToken ct) =>
		storage.GetListScheduleAsync(identity.Current.UserId, ct);
}
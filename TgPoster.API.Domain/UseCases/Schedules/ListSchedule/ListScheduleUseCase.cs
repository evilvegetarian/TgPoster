using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

internal sealed class ListScheduleUseCase(IListScheduleStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListScheduleQuery, List<ScheduleResponse>>
{
	public Task<List<ScheduleResponse>> Handle(ListScheduleQuery request, CancellationToken ct) =>
		storage.GetListScheduleAsync(identity.Current.UserId, ct);
}
using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

internal sealed class ListScheduleUseCase(IListScheduleStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListScheduleQuery, ScheduleListResponse>
{
	public async Task<ScheduleListResponse> Handle(ListScheduleQuery request, CancellationToken ct)
	{
		var items = await storage.GetListScheduleAsync(identity.Current.UserId, ct);
		return new ScheduleListResponse { Items = items };
	}
} 
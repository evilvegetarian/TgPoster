using MediatR;
using Security.IdentityServices;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Days.GetDays;

internal sealed class GetDaysUseCase(IGetDaysStorage storage, IIdentityProvider identity)
	: IRequestHandler<GetDaysQuery, DayListResponse>
{
	public async Task<DayListResponse> Handle(GetDaysQuery request, CancellationToken ct)
	{
		var existDays = await storage.ScheduleExistAsync(request.ScheduleId, identity.Current.UserId, ct);
		if (!existDays)
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		var items = await storage.GetDaysAsync(request.ScheduleId, ct);
		return new DayListResponse { Items = items };
	}
}
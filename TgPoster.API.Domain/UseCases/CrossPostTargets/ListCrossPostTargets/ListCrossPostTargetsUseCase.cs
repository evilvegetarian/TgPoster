using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.ListCrossPostTargets;

/// <summary>
///     Use case получения списка связок расписания
/// </summary>
internal sealed class ListCrossPostTargetsUseCase(IListCrossPostTargetsStorage storage, IIdentityProvider identity)
	: IRequestHandler<ListCrossPostTargetsQuery, List<CrossPostTargetResponse>>
{
	public async Task<List<CrossPostTargetResponse>> Handle(ListCrossPostTargetsQuery request, CancellationToken ct)
	{
		if (!await storage.ScheduleExistsAsync(request.ScheduleId, identity.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		return await storage.GetAsync(request.ScheduleId, ct);
	}
}

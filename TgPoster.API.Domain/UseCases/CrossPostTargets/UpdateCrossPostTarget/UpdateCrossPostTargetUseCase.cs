using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.UpdateCrossPostTarget;

/// <summary>
///     Use case обновления связки расписания
/// </summary>
internal sealed class UpdateCrossPostTargetUseCase(
	IUpdateCrossPostTargetStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<UpdateCrossPostTargetCommand>
{
	public async Task Handle(UpdateCrossPostTargetCommand request, CancellationToken ct)
	{
		if (!await storage.ScheduleExistsAsync(request.ScheduleId, identity.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		if (!await storage.ExistsAsync(request.Id, request.ScheduleId, ct))
		{
			throw new CrossPostTargetNotFoundException(request.Id);
		}

		await storage.UpdateAsync(request, ct);
	}
}

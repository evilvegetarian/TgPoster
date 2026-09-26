using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.DeleteCrossPostTarget;

/// <summary>
///     Use case удаления связки расписания
/// </summary>
internal sealed class DeleteCrossPostTargetUseCase(
	IDeleteCrossPostTargetStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<DeleteCrossPostTargetCommand>
{
	public async Task Handle(DeleteCrossPostTargetCommand request, CancellationToken ct)
	{
		if (!await storage.ScheduleExistsAsync(request.ScheduleId, identity.Current.UserId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		if (!await storage.ExistsAsync(request.Id, request.ScheduleId, ct))
		{
			throw new CrossPostTargetNotFoundException(request.Id);
		}

		await storage.DeleteAsync(request.Id, ct);
	}
}

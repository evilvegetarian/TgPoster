using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;

/// <summary>
///     Use case создания связки расписания с аккаунтом соцсети
/// </summary>
internal sealed class CreateCrossPostTargetUseCase(
	ICreateCrossPostTargetStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<CreateCrossPostTargetCommand, CreateCrossPostTargetResponse>
{
	public async Task<CreateCrossPostTargetResponse> Handle(CreateCrossPostTargetCommand request, CancellationToken ct)
	{
		var userId = identity.Current.UserId;

		if (!await storage.ScheduleExistsAsync(request.ScheduleId, userId, ct))
		{
			throw new ScheduleNotFoundException(request.ScheduleId);
		}

		if (!await storage.SocialAccountExistsAsync(request.SocialAccountId, userId, ct))
		{
			throw new SocialAccountNotFoundException(request.SocialAccountId);
		}

		if (await storage.TargetExistsAsync(request.ScheduleId, request.SocialAccountId, ct))
		{
			throw new CrossPostTargetAlreadyExistsException();
		}

		var id = await storage.CreateAsync(request, ct);

		return new CreateCrossPostTargetResponse { Id = id };
	}
}

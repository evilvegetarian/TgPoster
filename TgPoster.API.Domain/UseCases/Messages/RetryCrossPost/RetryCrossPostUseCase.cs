using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Messages.RetryCrossPost;

/// <summary>
///     Use case повтора неудавшегося или пропущенного кросс-поста
/// </summary>
internal sealed class RetryCrossPostUseCase(IRetryCrossPostStorage storage, IIdentityProvider identity)
	: IRequestHandler<RetryCrossPostCommand>
{
	public async Task Handle(RetryCrossPostCommand request, CancellationToken ct)
	{
		var userId = identity.Current.UserId;
		var info = await storage.GetRetryInfoAsync(request.MessageId, request.CrossPostId, userId, ct);
		if (info is null)
		{
			throw new CrossPostNotFoundException(request.CrossPostId);
		}

		if (info.Status is not (CrossPostStatus.Failed or CrossPostStatus.Skipped))
		{
			throw new CrossPostRetryNotAllowedException();
		}

		if (!info.AccountActive)
		{
			throw new SocialAccountInactiveException();
		}

		await storage.RetryAsync(request.CrossPostId, DateTimeOffset.UtcNow, ct);
	}
}

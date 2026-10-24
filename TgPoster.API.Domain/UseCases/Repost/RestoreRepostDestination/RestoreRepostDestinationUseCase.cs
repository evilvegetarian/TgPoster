using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.RestoreRepostDestination;

internal sealed class RestoreRepostDestinationUseCase(
	IRestoreRepostDestinationStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<RestoreRepostDestinationCommand>
{
	public async Task Handle(RestoreRepostDestinationCommand request, CancellationToken ct)
	{
		if (!await storage.SettingsExistsAsync(request.SettingsId, identity.Current.UserId, ct))
			throw new RepostSettingsNotFoundException(request.SettingsId);

		var removed = await storage.GetRemovedAsync(request.SettingsId, request.DestinationId, ct)
		              ?? throw new RepostDestinationNotFoundException(request.DestinationId);

		if (await storage.ChatInListAsync(request.SettingsId, removed.ChatId, ct))
			throw new RepostDestinationAlreadyExistsException(removed.Title ?? removed.ChatId.ToString());

		await storage.RestoreAsync(request.DestinationId, ct);
	}
}

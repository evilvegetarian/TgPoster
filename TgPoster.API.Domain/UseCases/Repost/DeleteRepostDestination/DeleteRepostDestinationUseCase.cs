using MediatR;
using Security.IdentityServices;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestination;

internal sealed class DeleteRepostDestinationUseCase(
	IDeleteRepostDestinationStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<DeleteRepostDestinationCommand, Unit>
{
	public async Task<Unit> Handle(DeleteRepostDestinationCommand request, CancellationToken ct)
	{
		if (!await storage.DestinationExistsAsync(request.Id, identity.Current.UserId, ct))
			throw new RepostDestinationNotFoundException(request.Id);

		await storage.DeleteDestinationAsync(request.Id, ct);

		return Unit.Value;
	}
}
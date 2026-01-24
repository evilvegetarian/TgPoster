using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestination;

internal sealed class DeleteRepostDestinationUseCase(IDeleteRepostDestinationStorage storage)
	: IRequestHandler<DeleteRepostDestinationCommand, Unit>
{
	public async Task<Unit> Handle(DeleteRepostDestinationCommand request, CancellationToken ct)
	{
		if (!await storage.DestinationExistsAsync(request.Id, ct))
			throw new RepostDestinationNotFoundException(request.Id);

		await storage.DeleteDestinationAsync(request.Id, ct);

		return Unit.Value;
	}
}

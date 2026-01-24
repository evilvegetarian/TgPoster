using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

internal sealed class UpdateRepostDestinationUseCase(IUpdateRepostDestinationStorage storage)
	: IRequestHandler<UpdateRepostDestinationCommand, Unit>
{
	public async Task<Unit> Handle(UpdateRepostDestinationCommand request, CancellationToken ct)
	{
		if (!await storage.DestinationExistsAsync(request.Id, ct))
			throw new RepostDestinationNotFoundException(request.Id);

		await storage.UpdateDestinationAsync(request.Id, request.IsActive, ct);

		return Unit.Value;
	}
}

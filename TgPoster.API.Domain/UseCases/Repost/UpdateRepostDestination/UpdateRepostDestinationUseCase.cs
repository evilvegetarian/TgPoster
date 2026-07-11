using MediatR;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

internal sealed class UpdateRepostDestinationUseCase(IUpdateRepostDestinationStorage storage)
	: IRequestHandler<UpdateRepostDestinationCommand>
{
	public async Task Handle(UpdateRepostDestinationCommand request, CancellationToken ct)
	{
		if (!await storage.DestinationExistsAsync(request.Id, ct))
			throw new RepostDestinationNotFoundException(request.Id);

		await storage.UpdateDestinationAsync(
			request.Id,
			request.IsActive,
			request.DelayMinSeconds,
			request.DelayMaxSeconds,
			request.RepostEveryNth,
			request.SkipProbability,
			request.MaxRepostsPerDay,
			ct);
	}
}
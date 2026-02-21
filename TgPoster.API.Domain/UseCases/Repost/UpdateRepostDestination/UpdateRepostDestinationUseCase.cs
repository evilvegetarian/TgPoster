using MediatR;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

internal sealed class UpdateRepostDestinationUseCase(IUpdateRepostDestinationStorage storage)
	: IRequestHandler<UpdateRepostDestinationCommand>
{
	public async Task Handle(UpdateRepostDestinationCommand request, CancellationToken ct)
	{
		ValidateRandomnessSettings(request);

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

	private static void ValidateRandomnessSettings(UpdateRepostDestinationCommand request)
	{
		if (request.DelayMinSeconds < 0)
			throw new InvalidRepostSettingsException("DelayMinSeconds не может быть отрицательным");

		if (request.DelayMaxSeconds < request.DelayMinSeconds)
			throw new InvalidRepostSettingsException("DelayMaxSeconds не может быть меньше DelayMinSeconds");

		if (request.RepostEveryNth < 1)
			throw new InvalidRepostSettingsException("RepostEveryNth должен быть >= 1");

		if (request.SkipProbability is < 0 or > 100)
			throw new InvalidRepostSettingsException("SkipProbability должен быть от 0 до 100");

		if (request.MaxRepostsPerDay is < 1)
			throw new InvalidRepostSettingsException("MaxRepostsPerDay должен быть >= 1");
	}
}

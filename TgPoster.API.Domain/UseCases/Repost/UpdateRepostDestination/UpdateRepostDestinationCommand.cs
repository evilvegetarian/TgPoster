using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

public sealed record UpdateRepostDestinationCommand(
	Guid Id,
	bool IsActive,
	int DelayMinSeconds,
	int DelayMaxSeconds,
	int RepostEveryNth,
	int SkipProbability,
	int? MaxRepostsPerDay) : IRequest;

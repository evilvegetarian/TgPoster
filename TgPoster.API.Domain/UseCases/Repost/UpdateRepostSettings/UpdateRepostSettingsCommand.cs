using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostSettings;

public sealed record UpdateRepostSettingsCommand(
	Guid Id,
	bool IsActive,
	int DefaultDelayMinSeconds,
	int DefaultDelayMaxSeconds,
	int DefaultRepostEveryNth,
	int DefaultSkipProbability,
	int? DefaultMaxRepostsPerDay) : IRequest;

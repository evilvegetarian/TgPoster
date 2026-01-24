using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

public sealed record UpdateRepostDestinationCommand(
	Guid Id,
	bool IsActive) : IRequest<Unit>;

using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestination;

public sealed record DeleteRepostDestinationCommand(Guid Id) : IRequest<Unit>;

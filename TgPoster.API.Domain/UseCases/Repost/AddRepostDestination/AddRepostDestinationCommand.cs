using MediatR;

namespace TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;

public sealed record AddRepostDestinationCommand(
	Guid RepostSettingsId,
	string ChatIdentifier) : IRequest<AddRepostDestinationResponse>;

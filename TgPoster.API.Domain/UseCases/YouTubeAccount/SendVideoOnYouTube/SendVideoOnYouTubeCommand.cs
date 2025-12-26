using MediatR;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;

public record SendVideoOnYouTubeCommand(Guid MessageId) : IRequest;
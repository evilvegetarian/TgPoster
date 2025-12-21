using MediatR;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;

public record CallBackYouTubeQuery(string Code, string State, string CallBack) : IRequest;
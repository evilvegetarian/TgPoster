using MediatR;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.DeleteYouTubeAccount;

public sealed record DeleteYouTubeAccountCommand(Guid Id) : IRequest;

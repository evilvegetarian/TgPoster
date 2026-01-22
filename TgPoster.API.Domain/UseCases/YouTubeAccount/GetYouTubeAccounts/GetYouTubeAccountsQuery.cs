using MediatR;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;

public record GetYouTubeAccountsQuery : IRequest<List<YouTubeAccountResponse>>;
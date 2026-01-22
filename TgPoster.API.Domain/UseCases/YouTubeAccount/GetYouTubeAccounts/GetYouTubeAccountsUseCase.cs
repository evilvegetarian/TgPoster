using MediatR;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;

public class GetYouTubeAccountsUseCase(IGetYouTubeAccountsStorage storage)
	: IRequestHandler<GetYouTubeAccountsQuery, List<YouTubeAccountResponse>>
{
	public async Task<List<YouTubeAccountResponse>> Handle(
		GetYouTubeAccountsQuery request,
		CancellationToken cancellationToken
	) => await storage.GetYouTubeAccounts(cancellationToken);
}
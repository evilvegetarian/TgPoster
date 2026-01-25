using MediatR;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;

public class GetYouTubeAccountsUseCase(IGetYouTubeAccountsStorage storage)
	: IRequestHandler<GetYouTubeAccountsQuery, YouTubeAccountListResponse>
{
	public async Task<YouTubeAccountListResponse> Handle(
		GetYouTubeAccountsQuery request,
		CancellationToken cancellationToken
	)
	{
		var items = await storage.GetYouTubeAccounts(cancellationToken);
		return new YouTubeAccountListResponse { Items = items };
	}
}
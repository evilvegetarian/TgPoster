using MediatR;
using Security.IdentityServices;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;

public class GetYouTubeAccountsUseCase(IGetYouTubeAccountsStorage storage, IIdentityProvider identity)
	: IRequestHandler<GetYouTubeAccountsQuery, YouTubeAccountListResponse>
{
	public async Task<YouTubeAccountListResponse> Handle(
		GetYouTubeAccountsQuery request,
		CancellationToken cancellationToken
	)
	{
		var items = await storage.GetYouTubeAccounts(identity.Current.UserId, cancellationToken);
		return new YouTubeAccountListResponse { Items = items };
	}
}
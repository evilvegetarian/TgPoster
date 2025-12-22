using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using MediatR;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;

public class CallBackYouTubeUseCase(ICallBackYouTubeStorage storage, IIdentityProvider provider)
	: IRequestHandler<CallBackYouTubeQuery>
{
	public async Task Handle(CallBackYouTubeQuery request, CancellationToken ct)
	{
		var accountYouTubeGuid = Guid.Parse(request.State);
		var (clientId, clientSecret) = await storage.GetClients(accountYouTubeGuid, provider.Current.UserId, ct);
		var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
		{
			ClientSecrets = new ClientSecrets
			{
				ClientId = clientId,
				ClientSecret = clientSecret
			}
		});

		var token = await flow.ExchangeCodeForTokenAsync(
			userId: "user-id",
			code: request.Code,
			redirectUri: request.CallBack,
			ct
		);
		await storage.AddToken(accountYouTubeGuid, token.AccessToken, ct);
	}
}
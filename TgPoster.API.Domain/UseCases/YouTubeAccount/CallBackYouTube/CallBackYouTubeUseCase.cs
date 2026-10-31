using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MediatR;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;

public class CallBackYouTubeUseCase(ICallBackYouTubeStorage storage, YouTubeOAuthStateService states)
	: IRequestHandler<CallBackYouTubeQuery>
{
	public async Task Handle(CallBackYouTubeQuery request, CancellationToken ct)
	{
		if (string.IsNullOrEmpty(request.ExpectedState) || request.State != request.ExpectedState)
			throw new InvalidYouTubeOAuthStateException();
		var state = states.Take(request.State) ?? throw new InvalidYouTubeOAuthStateException();
		var accountYouTubeGuid = state.AccountId;
		var (clientId, clientSecret) = await storage.GetClients(accountYouTubeGuid, state.UserId, ct);
		if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
			throw new YouTubeAccountNotFoundException(accountYouTubeGuid);

		var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
		{
			ClientSecrets = new ClientSecrets
			{
				ClientId = clientId,
				ClientSecret = clientSecret
			},
			Scopes = [YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeReadonly]
		});

		var token = await flow.ExchangeCodeForTokenAsync(
			"user-id",
			request.Code,
			request.CallBack,
			ct
		);

		var credential = new UserCredential(flow, "user-id", token);
		var youtubeService = new YouTubeService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "TgPoster"
		});

		var channelsRequest = youtubeService.Channels.List("snippet");
		channelsRequest.Mine = true;
		var channelsResponse = await channelsRequest.ExecuteAsync(ct);

		var channel = channelsResponse.Items?.FirstOrDefault();
		var channelName = channel?.Snippet?.Title ?? "Unknown Channel";
		var channelId = channel?.Id;

		await storage.AddToken(
			accountYouTubeGuid,
			state.UserId,
			token.AccessToken,
			token.RefreshToken,
			channelName,
			channelId,
			ct
		);
	}
}

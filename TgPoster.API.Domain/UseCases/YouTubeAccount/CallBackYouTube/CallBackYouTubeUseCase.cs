using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MediatR;
using TgPoster.API.Domain.Services;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;

public class CallBackYouTubeUseCase(ICallBackYouTubeStorage storage, YouTubeOAuthStateService states,
	Google.Apis.Http.IHttpClientFactory? httpClientFactory = null)
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

		var flowInitializer = new GoogleAuthorizationCodeFlow.Initializer
		{
			ClientSecrets = new ClientSecrets
			{
				ClientId = clientId,
				ClientSecret = clientSecret
			},
			Scopes = [YouTubeService.Scope.YoutubeUpload, YouTubeService.Scope.YoutubeReadonly]
		};
		if (httpClientFactory is not null)
			flowInitializer.HttpClientFactory = httpClientFactory;
		var flow = new GoogleAuthorizationCodeFlow(flowInitializer);

		var token = await flow.ExchangeCodeForTokenAsync(
			"user-id",
			request.Code,
			request.CallBack,
			ct
		);

		var credential = new UserCredential(flow, "user-id", token);
		var serviceInitializer = new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "TgPoster"
		};
		if (httpClientFactory is not null)
			serviceInitializer.HttpClientFactory = httpClientFactory;
		var youtubeService = new YouTubeService(serviceInitializer);

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

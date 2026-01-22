using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.YouTube.v3;
using MediatR;
using Security.Interfaces;
using TgPoster.API.Domain.Exceptions;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;

public class LoginYouTubeUseCase(ILoginYouTubeStorage storage, IIdentityProvider provider)
	: IRequestHandler<LoginYouTubeCommand, CreateYouTubeAccountResponse>
{
	public async Task<CreateYouTubeAccountResponse> Handle(LoginYouTubeCommand request, CancellationToken ct)
	{
		var clientSecrets = new ClientSecrets();
		if (request.JsonFile != null)
		{
			await using var memoryStream = new MemoryStream();
			await request.JsonFile.CopyToAsync(memoryStream, ct);
			memoryStream.Position = 0;
			clientSecrets = (await GoogleClientSecrets.FromStreamAsync(memoryStream, ct)).Secrets;
		}
		else
		{
			clientSecrets.ClientId = request.ClientId;
			clientSecrets.ClientSecret = request.ClientSecret;
		}

		var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
		{
			ClientSecrets = clientSecrets,
			Scopes =
			[
				YouTubeService.Scope.YoutubeUpload,
				YouTubeService.Scope.YoutubeReadonly
			]
		});

		if (clientSecrets?.ClientSecret is null || clientSecrets.ClientId is null)
		{
			throw new YouTubeClientSecretsNotFoundException();
		}

		var guid = await storage.CreateYouTubeAccountAsync("", clientSecrets.ClientId, clientSecrets.ClientSecret,
			provider.Current.UserId, ct);
		var authRequestUrl = flow.CreateAuthorizationCodeRequest(request.RedirectUrl);
		authRequestUrl.State = guid.ToString();
		return new CreateYouTubeAccountResponse { Url = authRequestUrl.Build().ToString() };
	}
}
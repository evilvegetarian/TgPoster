using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.YouTube.v3;
using MediatR;
using Microsoft.AspNetCore.Http;
using Security.Interfaces;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;

public record LoginYouTubeCommand(IFormFile? JsonFile, string? ClientId, string? ClientSecret, string RedirectUrl)
	: IRequest<string>;

public class LoginYouTubeUseCase(ILoginYouTubeStorage storage, IIdentityProvider provider)
	: IRequestHandler<LoginYouTubeCommand, string>
{
	public async Task<string> Handle(LoginYouTubeCommand request, CancellationToken ct)
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
			Scopes = [YouTubeService.Scope.YoutubeUpload]
		});
		
		if (clientSecrets?.ClientSecret is null || clientSecrets.ClientId is null)
			throw new InvalidOperationException("YouTubeService client secret not found");

		await storage.CreateYouTubeAccountAsync("", clientSecrets.ClientId, clientSecrets.ClientSecret, provider.Current.UserId, ct);

		return flow.CreateAuthorizationCodeRequest(request.RedirectUrl).Build().ToString();
	}
}

public interface ILoginYouTubeStorage
{
	Task<Guid> CreateYouTubeAccountAsync(
		string name,
		string clientId,
		string clientSecret,
		Guid userId,
		CancellationToken ct
	);
}
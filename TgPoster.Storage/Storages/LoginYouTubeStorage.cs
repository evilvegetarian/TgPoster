using TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

internal class LoginYouTubeStorage(GuidFactory guidFactory, PosterContext context) : ILoginYouTubeStorage
{
	public async Task<Guid> CreateYouTubeAccountAsync(
		string name,
		string clientId,
		string clientSecret,
		Guid userId,
		CancellationToken ct
	)
	{
		var youTube = new YouTubeAccount
		{
			Id = guidFactory.New(),
			ClientId = clientId,
			ClientSecret = clientSecret,
			Name = name,
			UserId = userId,
			AccessToken = ""
		};
		await context.AddAsync(youTube, ct);
		await context.SaveChangesAsync(ct);
		return youTube.Id;
	}
}
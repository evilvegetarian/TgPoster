namespace TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;

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
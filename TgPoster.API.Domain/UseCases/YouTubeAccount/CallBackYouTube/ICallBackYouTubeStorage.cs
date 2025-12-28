namespace TgPoster.API.Domain.UseCases.YouTubeAccount.CallBackYouTube;

public interface ICallBackYouTubeStorage
{
	Task<(string ClientId, string ClientSecret)> GetClients(Guid accountYouTubeId, Guid userId, CancellationToken ct);
	Task AddToken(Guid accountYouTubeId, string accessToken, string? refreshToken, CancellationToken ct);
}

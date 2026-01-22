using Shared.YouTube;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;

public interface ISendVideoOnYouTubeStorage
{
	Task<List<FileDto>> GetVideoFileMessageAsync(Guid messageId, Guid userId, CancellationToken ct);
	Task<YouTubeAccountDto?> GetAccessTokenAsync(Guid messageId, Guid userId, CancellationToken ct);

	Task UpdateYouTubeTokensAsync(
		Guid youTubeAccountId,
		string accessToken,
		string? refreshToken,
		CancellationToken ct
	);
}
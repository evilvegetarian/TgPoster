namespace TgPoster.API.Domain.UseCases.YouTubeAccount.DeleteYouTubeAccount;

public interface IDeleteYouTubeAccountStorage
{
	Task<bool> ExistsAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken);
	Task DeleteYouTubeAccountAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken);
}
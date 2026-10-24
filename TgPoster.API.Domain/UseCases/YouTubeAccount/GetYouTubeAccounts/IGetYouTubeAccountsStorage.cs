namespace TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;

public interface IGetYouTubeAccountsStorage
{
	Task<List<YouTubeAccountResponse>> GetYouTubeAccounts(Guid userId, CancellationToken ct);
}
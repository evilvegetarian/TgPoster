using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class GetYouTubeAccountsStorage(PosterContext context) : IGetYouTubeAccountsStorage
{
	public async Task<List<YouTubeAccountResponse>> GetYouTubeAccounts(CancellationToken ct)
	{
		return await context.YouTubeAccounts
			.AsNoTracking()
			.Select(x => new YouTubeAccountResponse
			{
				Id = x.Id,
				Name = x.Name,
				DefaultTitle = x.DefaultTitle,
				DefaultDescription = x.DefaultDescription,
				DefaultTags = x.DefaultTags,
				AutoPostingVideo = x.AutoPostingVideo
			})
			.ToListAsync(ct);
	}
}
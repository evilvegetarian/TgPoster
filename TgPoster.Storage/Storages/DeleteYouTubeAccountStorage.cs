using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.YouTubeAccount.DeleteYouTubeAccount;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class DeleteYouTubeAccountStorage(PosterContext context) : IDeleteYouTubeAccountStorage
{
	public Task<bool> ExistsAsync(Guid id, Guid currentUserId, CancellationToken ct)
	{
		return context.YouTubeAccounts.AnyAsync(x => x.Id == id && x.UserId == currentUserId, ct);
	}

	public async Task DeleteYouTubeAccountAsync(Guid id, Guid userId, CancellationToken ct)
	{
		var account = await context.YouTubeAccounts
			.Where(x => x.Id == id && x.UserId == userId)
			.FirstOrDefaultAsync(ct);
		context.YouTubeAccounts.Remove(account!);
		await context.SaveChangesAsync(ct);
	}
}

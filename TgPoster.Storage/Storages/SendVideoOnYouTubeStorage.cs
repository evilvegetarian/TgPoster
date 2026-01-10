using Microsoft.EntityFrameworkCore;
using Shared;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class SendVideoOnYouTubeStorage(PosterContext context) : ISendVideoOnYouTubeStorage
{
	public Task<List<FileDto>> GetVideoFileMessageAsync(Guid messageId, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.Where(x => x.Id == messageId)
			.SelectMany(x => x.MessageFiles)
			.Select(file => new FileDto
			{
				Id = file.Id,
				ContentType = file.ContentType,
				TgFileId = file.TgFileId,
				Previews = file.Thumbnails.Select(x => new PreviewDto
				{
					Id = x.Id,
					TgFileId = x.TgFileId
				}).ToList()
			})
			.ToListAsync(ct);
	}

	public Task<YouTubeAccountDto?> GetAccessTokenAsync(Guid messageId, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.AsNoTracking()
			.Where(x => x.Id == messageId)
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => x.Schedule.YouTubeAccount)
			.Select(x => x != null ? new YouTubeAccountDto
			{
				Id = x.Id,
				AccessToken = x.AccessToken,
				RefreshToken = x.RefreshToken,
				ClientId = x.ClientId,
				ClientSecret = x.ClientSecret
			} : null)
			.FirstOrDefaultAsync(ct);
	}

	public async Task UpdateYouTubeTokensAsync(Guid youTubeAccountId, string accessToken, string? refreshToken, CancellationToken ct)
	{
		var account = await context.YouTubeAccounts
			.Where(x => x.Id == youTubeAccountId)
			.FirstOrDefaultAsync(ct);

		if (account != null)
		{
			account.AccessToken = accessToken;
			account.RefreshToken = refreshToken;
			await context.SaveChangesAsync(ct);
		}
	}
}

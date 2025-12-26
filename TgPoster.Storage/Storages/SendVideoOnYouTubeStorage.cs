using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

public class SendVideoOnYouTubeStorage(PosterContext context) : ISendVideoOnYouTubeStorage
{
	public async Task<List<FileDto>> GetVideoFileMessageAsync(Guid messageId, Guid userId, CancellationToken ct)
	{
		var files = await context.Messages
			.Where(x => x.Id == messageId)
			.SelectMany(x => x.MessageFiles)
			.ToListAsync(ct);
		return files.Select(f => new FileDto
		{
			Id = f.Id,
			ContentType = f.ContentType,
			TgFileId = f.TgFileId,
			PreviewIds = f is VideoMessageFile videoFile
				? videoFile.ThumbnailIds.ToList()
				: []
		}).ToList();
	}

	public Task<string?> GetAccessTokenAsync(Guid messageId, Guid userId, CancellationToken ct)
	{
		return context.Messages
			.AsNoTracking()
			.Where(x => x.Id == messageId)
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => x.Schedule.YouTubeAccount)
			.Select(x => x != null ? x.AccessToken : null)
			.FirstOrDefaultAsync(ct);
	}
}
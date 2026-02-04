using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Worker.Domain.UseCases.CommentRepostMonitor;

namespace TgPoster.Storage.Storages.CommentRepost;

internal sealed class CommentRepostMonitorStorage(PosterContext context) : ICommentRepostMonitorStorage
{
	public Task<List<CommentRepostSettingDto>> GetActiveSettingsAsync(CancellationToken ct)
	{
		return context.CommentRepostSettings
			.Where(x => x.IsActive)
			.Select(x => new CommentRepostSettingDto
			{
				Id = x.Id,
				WatchedChannelId = x.WatchedChannelId,
				WatchedChannelAccessHash = x.WatchedChannelAccessHash,
				DiscussionGroupId = x.DiscussionGroupId,
				DiscussionGroupAccessHash = x.DiscussionGroupAccessHash,
				TelegramSessionId = x.TelegramSessionId,
				SourceChannelId = x.Schedule.ChannelId,
				LastProcessedPostId = x.LastProcessedPostId
			})
			.ToListAsync(ct);
	}

	public async Task UpdateLastProcessedAsync(Guid settingsId, int lastPostId, CancellationToken ct)
	{
		var settings = await context.CommentRepostSettings.FirstAsync(x => x.Id == settingsId, ct);
		settings.LastProcessedPostId = lastPostId;
		settings.LastCheckDate = DateTime.UtcNow;
		await context.SaveChangesAsync(ct);
	}
}

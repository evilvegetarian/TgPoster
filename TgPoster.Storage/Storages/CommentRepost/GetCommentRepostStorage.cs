using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.CommentRepost.GetCommentRepost;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.CommentRepost;

internal sealed class GetCommentRepostStorage(PosterContext context) : IGetCommentRepostStorage
{
	public Task<GetCommentRepostResponse?> GetAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.CommentRepostSettings
			.Where(x => x.Id == id && x.Schedule.UserId == userId)
			.Select(x => new GetCommentRepostResponse
			{
				Id = x.Id,
				WatchedChannel = x.WatchedChannel,
				WatchedChannelId = x.WatchedChannelId,
				ScheduleId = x.ScheduleId,
				ScheduleName = x.Schedule.Name,
				TelegramSessionId = x.TelegramSessionId,
				TelegramSessionName = x.TelegramSession.Name,
				IsActive = x.IsActive,
				LastProcessedPostId = x.LastProcessedPostId,
				LastCheckDate = x.LastCheckDate,
				Created = x.Created!.Value
			})
			.FirstOrDefaultAsync(ct);
	}
}

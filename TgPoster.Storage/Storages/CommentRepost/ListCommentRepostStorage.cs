using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.CommentRepost.ListCommentRepost;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.CommentRepost;

internal sealed class ListCommentRepostStorage(PosterContext context) : IListCommentRepostStorage
{
	public Task<List<CommentRepostItemDto>> GetListAsync(Guid userId, CancellationToken ct)
	{
		return context.CommentRepostSettings
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new CommentRepostItemDto
			{
				Id = x.Id,
				WatchedChannel = x.WatchedChannel,
				ScheduleId = x.ScheduleId,
				ScheduleName = x.Schedule.Name,
				IsActive = x.IsActive,
				LastCheckDate = x.LastCheckDate
			})
			.ToListAsync(ct);
	}
}

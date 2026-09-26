using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Messages.RetryCrossPost;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.CrossPosting;

/// <summary>
///     Хранилище повтора кросс-постов
/// </summary>
internal sealed class RetryCrossPostStorage(PosterContext context) : IRetryCrossPostStorage
{
	public Task<CrossPostRetryInfo?> GetRetryInfoAsync(
		Guid messageId,
		Guid crossPostId,
		Guid userId,
		CancellationToken ct)
	{
		return context.CrossPosts
			.AsNoTracking()
			.Where(cp => cp.Id == crossPostId)
			.Where(cp => cp.MessageId == messageId)
			.Where(cp => cp.Message.Schedule.UserId == userId)
			.Select(cp => new CrossPostRetryInfo
			{
				Status = cp.Status,
				AccountActive = context.SocialAccounts
					.Any(x => x.Id == cp.SocialAccountId && x.Status == SocialAccountStatus.Active)
			})
			.FirstOrDefaultAsync(ct);
	}

	public async Task RetryAsync(Guid crossPostId, DateTimeOffset now, CancellationToken ct)
	{
		var crossPost = await context.CrossPosts.FirstAsync(x => x.Id == crossPostId, ct);

		crossPost.Status = CrossPostStatus.Pending;
		crossPost.Attempts = 0;
		crossPost.ScheduledAt = now;
		crossPost.StartedAt = null;
		crossPost.Error = null;

		await context.SaveChangesAsync(ct);
	}
}

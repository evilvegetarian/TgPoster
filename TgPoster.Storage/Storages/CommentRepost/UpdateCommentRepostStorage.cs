using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.CommentRepost.UpdateCommentRepost;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.CommentRepost;

internal sealed class UpdateCommentRepostStorage(PosterContext context) : IUpdateCommentRepostStorage
{
	public Task<bool> ExistsAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.CommentRepostSettings
			.Include(x => x.Schedule)
			.AnyAsync(x => x.Id == id && x.Schedule.UserId == userId, ct);
	}

	public async Task UpdateAsync(Guid id, bool isActive, CancellationToken ct)
	{
		var settings = await context.CommentRepostSettings.FirstAsync(x => x.Id == id, ct);
		settings.IsActive = isActive;
		await context.SaveChangesAsync(ct);
	}

	public async Task RemoveAccountBanAsync(Guid id, CancellationToken ct)
	{
		var settings = await context.CommentRepostSettings
			.Where(x => x.Id == id)
			.Select(x => new { x.TelegramSessionId, x.DiscussionGroupId })
			.FirstAsync(ct);

		await context.RemoveChatBanAsync(settings.TelegramSessionId, settings.DiscussionGroupId, ct);
		await context.SaveChangesAsync(ct);
	}
}

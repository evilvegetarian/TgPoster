using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.CommentRepost.UpdateCommentRepost;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

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
}

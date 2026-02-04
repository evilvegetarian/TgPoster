using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.CommentRepost.DeleteCommentRepost;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.CommentRepost;

internal sealed class DeleteCommentRepostStorage(PosterContext context) : IDeleteCommentRepostStorage
{
	public Task<bool> ExistsAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.CommentRepostSettings
			.Include(x => x.Schedule)
			.AnyAsync(x => x.Id == id && x.Schedule.UserId == userId, ct);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct)
	{
		var settings = await context.CommentRepostSettings.FirstAsync(x => x.Id == id, ct);
		context.Remove(settings);
		await context.SaveChangesAsync(ct);
	}
}

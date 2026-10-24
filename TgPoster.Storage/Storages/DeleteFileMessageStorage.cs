using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Messages.DeleteFileMessage;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class DeleteFileMessageStorage(PosterContext context) : IDeleteFileMessageStorage
{
	public async Task<bool> ExistMessageAsync(Guid messageId, Guid userId, CancellationToken ct)
	{
		return await context.Messages
			.Where(m => m.Id == messageId)
			.Where(m => m.Schedule.UserId == userId)
			.AnyAsync(ct);
	}

	public async Task DeleteFileAsync(Guid messageId, Guid fileId, CancellationToken ct)
	{
		// Файл ищется только среди файлов поста: владелец проверен по посту, а не по файлу
		var file = await context.MessageFiles
			.FirstOrDefaultAsync(f => f.Id == fileId && f.MessageId == messageId, ct);
		if (file != null)
		{
			context.MessageFiles.Remove(file);
			await context.SaveChangesAsync(ct);
		}
	}
}
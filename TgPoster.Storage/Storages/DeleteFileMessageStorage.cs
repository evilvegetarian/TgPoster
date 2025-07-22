using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Messages.DeleteFileMessage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Exception;

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

    public async Task DeleteFileAsync(Guid fileId, CancellationToken ct)
    {
        var file = await context.MessageFiles.FindAsync(fileId, ct);
        if (file != null)
        {
            context.MessageFiles.Remove(file);
            await context.SaveChangesAsync(ct);
        }
    }
}
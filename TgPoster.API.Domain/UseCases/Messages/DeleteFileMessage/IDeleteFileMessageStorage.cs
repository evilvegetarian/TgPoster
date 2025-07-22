using TgPoster.API.Domain.Models;

namespace TgPoster.API.Domain.UseCases.Messages.DeleteFileMessage;

public interface IDeleteFileMessageStorage
{
    Task<bool> ExistMessageAsync(Guid messageId, Guid userId, CancellationToken ct);
    Task DeleteFileAsync(Guid fileId, CancellationToken ct);
}

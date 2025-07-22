using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;

namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

public interface IEditMessageStorage
{
    Task<bool> ExistMessageAsync(Guid messageId, Guid userId, CancellationToken ct);
    Task<List<FileDto>> GetFilesAsync(List<Guid> fileIds, CancellationToken ct);
    Task UpdateMessageAsync(EditMessageCommand message, List<MediaFileResult> files, CancellationToken ct);
    Task<TelegramBotDto?> GetTelegramBotAsync(Guid requestId, Guid userId, CancellationToken ct);
}

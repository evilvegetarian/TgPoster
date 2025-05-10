using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Messages.GetMessageById;

public interface IGetMessageStorage
{
    Task<MessageDto?> GetMessagesAsync(Guid id, Guid userId, CancellationToken ct);
}
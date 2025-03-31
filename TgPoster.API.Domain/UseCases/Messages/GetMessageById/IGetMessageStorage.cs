using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Messages.GetMessageById;

public interface IGetMessageStorage
{
    Task<MessageDto?> GetMessage(Guid id, Guid userId, CancellationToken cancellationToken);
}
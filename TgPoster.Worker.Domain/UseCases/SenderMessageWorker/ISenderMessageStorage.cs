namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

public interface ISenderMessageStorage
{
    Task<List<MessageDetail>> GetMessagesAsync();
    Task UpdateStatusMessage(Guid id);
}
namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

public interface ISenderMessageStorage
{
    Task<List<MessageDetail>> GetMessagesAsync();
    Task UpdateStatusMessageAsync(Guid id);
    Task UpdateStatusInHandleMessageAsync(List<Guid> ids);
}
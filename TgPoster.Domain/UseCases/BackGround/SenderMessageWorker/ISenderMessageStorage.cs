namespace TgPoster.Domain.UseCases.BackGround.SenderMessageWorker;

public interface ISenderMessageStorage
{
    Task<List<MessageDetail>> GetMessagesAsync();
    Task UpdateStatusMessage(Guid id);
}
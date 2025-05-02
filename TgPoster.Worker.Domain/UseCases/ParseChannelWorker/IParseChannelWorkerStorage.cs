namespace TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

public interface IParseChannelWorkerStorage
{
    Task<List<Guid>> GetParameters();
    Task SetWaitingStatus(Guid id);
    Task SetInHandleStatus(List<Guid> ids);
}
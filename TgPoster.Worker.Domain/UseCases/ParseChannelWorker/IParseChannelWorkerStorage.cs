namespace TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

public interface IParseChannelWorkerStorage
{
    Task<List<Guid>> GetChannelParsingParametersAsync();
    Task SetWaitingStatusAsync(Guid id);
    Task SetInHandleStatusAsync(List<Guid> ids);
}
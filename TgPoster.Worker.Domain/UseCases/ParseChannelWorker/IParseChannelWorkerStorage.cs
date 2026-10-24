namespace TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

public interface IParseChannelWorkerStorage
{
	Task<List<Guid>> GetChannelParsingParametersAsync();
	Task SetInHandleStatusAsync(List<Guid> ids);
	Task<int> ReleaseStuckInHandleAsync();
	Task SetErrorStatusAsync(Guid id);
	Task<Guid?> GetScheduleIdAsync(Guid id, CancellationToken ct);
}
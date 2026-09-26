namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public interface IDiscoverChannelLinksStorage
{
	Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(int channelBatchSize, CancellationToken ct);
	Task UpsertAsync(DiscoveredPeerUpsert upsert, CancellationToken ct);
	Task BulkUpsertAsync(IReadOnlyCollection<DiscoveredPeerUpsert> upserts, CancellationToken ct);
	Task MarkAsSkippedAsync(Guid id, CancellationToken ct);

	/// <summary>
	///     Пометить канал ошибкой: он выпадет из очереди парсинга, дата последнего парсинга не меняется
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	Task MarkAsErrorAsync(Guid id, CancellationToken ct);

	Task ChannelBanned(Guid id, CancellationToken ct = default);
}
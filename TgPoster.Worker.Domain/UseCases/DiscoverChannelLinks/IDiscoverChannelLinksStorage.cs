using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public interface IDiscoverChannelLinksStorage
{
	Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(int channelBatchSize, CancellationToken ct);
	Task UpsertAsync(DiscoveredPeerUpsert upsert, CancellationToken ct);
	Task BulkUpsertAsync(IReadOnlyCollection<DiscoveredPeerUpsert> upserts, CancellationToken ct);
	Task MarkAsSkippedAsync(Guid id, CancellationToken ct);
	Task ChannelBanned(Guid id, CancellationToken ct = default);
}
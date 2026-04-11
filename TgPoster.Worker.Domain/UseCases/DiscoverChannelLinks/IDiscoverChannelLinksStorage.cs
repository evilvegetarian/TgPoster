namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public interface IDiscoverChannelLinksStorage
{
	Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(CancellationToken ct);

	Task<bool> ExistsAsync(string? username, long? telegramId, string? inviteHash, CancellationToken ct);

	Task UpsertAsync(DiscoveredPeerUpsert upsert, CancellationToken ct);
}

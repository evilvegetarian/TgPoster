using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public interface IDiscoverChannelLinksStorage
{
	Task<Guid?> GetSessionIdByPurposeAsync(TelegramSessionPurpose purpose, CancellationToken ct);

	Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(int channelBatchSize, CancellationToken ct);

	Task<bool> ExistsAsync(string? username, long? telegramId, string? inviteHash, CancellationToken ct);

	Task UpsertAsync(DiscoveredPeerUpsert upsert, CancellationToken ct);
	Task ChannelBanned(Guid id, CancellationToken ct = default);
}
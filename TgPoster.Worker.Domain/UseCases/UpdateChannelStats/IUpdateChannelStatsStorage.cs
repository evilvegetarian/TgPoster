using Shared.Enums;

namespace TgPoster.Worker.Domain.UseCases.UpdateChannelStats;

public interface IUpdateChannelStatsStorage
{
	Task<Guid?> GetSessionIdByPurposeAsync(TelegramSessionPurpose purpose, CancellationToken ct);
	Task<List<ChannelStatsDto>> GetChannelsToUpdateAsync(int batchSize, CancellationToken ct);
	Task UpdateParticipantsCountAsync(Guid id, int count, CancellationToken ct);
}

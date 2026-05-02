using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Worker.Domain.UseCases.UpdateChannelStats;

namespace TgPoster.Storage.Storages.UpdateChannelStats;

internal sealed class UpdateChannelStatsStorage(PosterContext context) : IUpdateChannelStatsStorage
{
	public Task<Guid?> GetSessionIdByPurposeAsync(TelegramSessionPurpose purpose, CancellationToken ct)
		=> context.TelegramSessions
			.Where(s => s.IsActive && s.Purposes.Contains(purpose))
			.Select(s => (Guid?)s.Id)
			.FirstOrDefaultAsync(ct);

	public Task<List<ChannelStatsDto>> GetChannelsToUpdateAsync(int batchSize, CancellationToken ct)
		=> context.DiscoveredChannels
			.Where(c => !c.IsBanned && c.Username != null)
			.OrderBy(c => c.ParticipantsUpdatedAt)
			.Take(batchSize)
			.Select(c => new ChannelStatsDto(c.Id, c.Username!, c.TelegramId))
			.ToListAsync(ct);

	public async Task UpdateParticipantsCountAsync(Guid id, int count, CancellationToken ct)
	{
		var channel = await context.DiscoveredChannels.FirstAsync(c => c.Id == id, ct);
		channel.ParticipantsCount = count;
		channel.ParticipantsUpdatedAt = DateTimeOffset.UtcNow;
		await context.SaveChangesAsync(ct);
	}
}

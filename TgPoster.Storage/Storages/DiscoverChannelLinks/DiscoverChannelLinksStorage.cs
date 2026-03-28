using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

namespace TgPoster.Storage.Storages.DiscoverChannelLinks;

internal sealed class DiscoverChannelLinksStorage(PosterContext context, GuidFactory guidFactory)
	: IDiscoverChannelLinksStorage
{
	public Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(CancellationToken ct)
	{
		return context.DiscoveredChannels
			.Where(x => x.Status == DiscoveryStatus.Pending || x.Status == DiscoveryStatus.Completed)
			.OrderBy(x => x.LastDiscoveredAt)
			.Select(x => new DiscoverChannelDto
			{
				Username = x.Username,
				LastParsedId = x.LastParsedId
			})
			.ToListAsync(ct);
	}

	public Task<bool> ExistsAsync(string username, CancellationToken ct)
	{
		return context.DiscoveredChannels
			.AnyAsync(x => x.Username == username, ct);
	}

	public async Task UpsertAsync(
		string username,
		string? tgUrl,
		int? lastParsedId,
		long? telegramId,
		string? peerType,
		string? title,
		int? participantsCount,
		bool markAsCompleted = false,
		CancellationToken ct = default)
	{
		var existing = await context.DiscoveredChannels
			.FirstOrDefaultAsync(x => x.Username == username, ct);

		if (existing is not null)
		{
			if (tgUrl is not null)
				existing.TgUrl = tgUrl;
			if (lastParsedId is not null)
				existing.LastParsedId = lastParsedId;
			if (telegramId is not null)
				existing.TelegramId = telegramId;
			if (peerType is not null)
				existing.PeerType = peerType;
			if (title is not null)
				existing.Title = title;
			if (participantsCount is not null)
				existing.ParticipantsCount = participantsCount;

			if (markAsCompleted)
			{
				existing.Status = DiscoveryStatus.Completed;
				existing.LastDiscoveredAt = DateTimeOffset.UtcNow;
			}
		}
		else
		{
			context.DiscoveredChannels.Add(new DiscoveredChannel
			{
				Id = guidFactory.New(),
				Username = username,
				TgUrl = tgUrl,
				LastParsedId = lastParsedId,
				TelegramId = telegramId,
				PeerType = peerType,
				Title = title,
				ParticipantsCount = participantsCount,
				Status = DiscoveryStatus.Pending
			});
		}

		await context.SaveChangesAsync(ct);
	}
}

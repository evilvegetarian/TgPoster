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
		CancellationToken ct)
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
				Status = DiscoveryStatus.Pending
			});
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task UpdateLastParsedIdAsync(
		string username,
		int lastParsedId,
		long telegramId,
		string? peerType,
		CancellationToken ct)
	{
		var channel = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == username, ct);

		channel.LastParsedId = lastParsedId;
		channel.TelegramId = telegramId;
		if (peerType is not null)
			channel.PeerType = peerType;
		channel.Status = DiscoveryStatus.Completed;
		channel.LastDiscoveredAt = DateTimeOffset.UtcNow;

		await context.SaveChangesAsync(ct);
	}
}

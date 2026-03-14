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
		string? sourceUrl,
		int? lastParsedId,
		long? telegramId,
		CancellationToken ct)
	{
		var existing = await context.DiscoveredChannels
			.FirstOrDefaultAsync(x => x.Username == username, ct);

		if (existing is not null)
		{
			if (sourceUrl is not null)
				existing.TgUrl = sourceUrl;
			if (lastParsedId is not null)
				existing.LastParsedId = lastParsedId;
			if (telegramId is not null)
				existing.TelegramId = telegramId;
		}
		else
		{
			context.DiscoveredChannels.Add(new DiscoveredChannel
			{
				Id = guidFactory.New(),
				Username = username,
				TgUrl = sourceUrl,
				LastParsedId = lastParsedId,
				TelegramId = telegramId,
				Status = DiscoveryStatus.Pending
			});
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task UpdateLastParsedIdAsync(
		string username,
		int lastParsedId,
		long telegramId,
		CancellationToken ct)
	{
		var channel = await context.DiscoveredChannels
			.FirstAsync(x => x.Username == username, ct);

		channel.LastParsedId = lastParsedId;
		channel.TelegramId = telegramId;
		channel.Status = DiscoveryStatus.Completed;

		await context.SaveChangesAsync(ct);
	}
}

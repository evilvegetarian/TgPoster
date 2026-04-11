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
			.Where(x => x.Username != null)
			.Where(x => x.LastParsedId == null)
			.OrderBy(x => x.LastDiscoveredAt)
			.Take(150)
			.Select(x => new DiscoverChannelDto
			{
				Id = x.Id,
				Username = x.Username,
				TelegramId = x.TelegramId,
				InviteHash = x.InviteHash,
				LastParsedId = x.LastParsedId
			})
			.ToListAsync(ct);
	}

	public Task<bool> ExistsAsync(string? username, long? telegramId, string? inviteHash, CancellationToken ct)
	{
		return context.DiscoveredChannels
			.AnyAsync(x =>
				(telegramId != null && x.TelegramId == telegramId) ||
				(username != null && x.Username == username) ||
				(inviteHash != null && x.InviteHash == inviteHash), ct);
	}

	public async Task UpsertAsync(DiscoveredPeerUpsert upsert, CancellationToken ct)
	{
		var existing = await FindExistingAsync(upsert.Username, upsert.TelegramId, upsert.InviteHash, ct);

		if (existing is not null)
		{
			if (upsert.TgUrl is not null)
				existing.TgUrl = upsert.TgUrl;
			if (upsert.LastParsedId is not null)
				existing.LastParsedId = upsert.LastParsedId;
			if (upsert.TelegramId is not null)
				existing.TelegramId = upsert.TelegramId;
			if (upsert.PeerType is not null)
				existing.PeerType = upsert.PeerType;
			if (upsert.Title is not null)
				existing.Title = upsert.Title;
			if (upsert.ParticipantsCount is not null)
				existing.ParticipantsCount = upsert.ParticipantsCount;
			if (upsert.Username is not null && existing.Username is null)
				existing.Username = upsert.Username;
			if (upsert.InviteHash is not null && existing.InviteHash is null)
				existing.InviteHash = upsert.InviteHash;

			if (upsert.MarkAsCompleted)
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
				Username = upsert.Username,
				TgUrl = upsert.TgUrl,
				LastParsedId = upsert.LastParsedId,
				TelegramId = upsert.TelegramId,
				PeerType = upsert.PeerType,
				Title = upsert.Title,
				ParticipantsCount = upsert.ParticipantsCount,
				InviteHash = upsert.InviteHash,
				DiscoveredFromChannelId = upsert.DiscoveredFromChannelId,
				Status = DiscoveryStatus.Pending
			});
		}

		await context.SaveChangesAsync(ct);
	}

	private async Task<DiscoveredChannel?> FindExistingAsync(
		string? username, long? telegramId, string? inviteHash, CancellationToken ct)
	{
		if (telegramId is not null)
		{
			var byTelegramId = await context.DiscoveredChannels
				.FirstOrDefaultAsync(x => x.TelegramId == telegramId, ct);
			if (byTelegramId is not null)
				return byTelegramId;
		}

		if (username is not null)
		{
			var byUsername = await context.DiscoveredChannels
				.FirstOrDefaultAsync(x => x.Username == username, ct);
			if (byUsername is not null)
				return byUsername;
		}

		if (inviteHash is not null)
		{
			return await context.DiscoveredChannels
				.FirstOrDefaultAsync(x => x.InviteHash == inviteHash, ct);
		}

		return null;
	}
}

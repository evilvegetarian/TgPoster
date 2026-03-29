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
			.Where(x => x.Username != null || x.TelegramId != null)
			.OrderBy(x => x.LastDiscoveredAt)
			.Select(x => new DiscoverChannelDto
			{
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

	public async Task UpsertAsync(
		string? username,
		string? tgUrl,
		int? lastParsedId,
		long? telegramId,
		string? peerType,
		string? title,
		int? participantsCount,
		string? inviteHash = null,
		bool markAsCompleted = false,
		CancellationToken ct = default)
	{
		var existing = await FindExistingAsync(username, telegramId, inviteHash, ct);

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
			if (username is not null && existing.Username is null)
				existing.Username = username;
			if (inviteHash is not null && existing.InviteHash is null)
				existing.InviteHash = inviteHash;

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
				InviteHash = inviteHash,
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

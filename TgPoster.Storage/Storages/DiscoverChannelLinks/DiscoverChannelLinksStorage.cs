using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

namespace TgPoster.Storage.Storages.DiscoverChannelLinks;

internal sealed class DiscoverChannelLinksStorage(PosterContext context, GuidFactory guidFactory)
	: IDiscoverChannelLinksStorage
{
	public Task<Guid?> GetSessionIdByPurposeAsync(TelegramSessionPurpose purpose, CancellationToken ct)
		=> context.TelegramSessions
			.Where(s => s.IsActive
			            && s.Purposes.Contains(purpose))
			.Select(s => (Guid?)s.Id)
			.FirstOrDefaultAsync(ct);

	public Task<List<DiscoverChannelDto>> GetChannelsToProcessAsync(int channelBatchSize, CancellationToken ct)
	{
		return context.DiscoveredChannels
			.Where(x => x.Status == DiscoveryStatus.Pending || x.Status == DiscoveryStatus.Completed)
			.Where(x => x.Username != null)
			.OrderBy(x => x.LastDiscoveredAt != null)
			.Take(channelBatchSize)
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
				(telegramId != null && x.TelegramId == telegramId)
				|| (username != null && x.Username == username)
				|| (inviteHash != null && x.InviteHash == inviteHash), ct);
	}

	public async Task UpsertAsync(DiscoveredPeerUpsert upsert, CancellationToken ct)
	{
		var existing = await FindExistingAsync(upsert.Username, upsert.TelegramId, upsert.InviteHash, ct);

		if (existing is not null)
		{
			ApplyUpsertFields(existing, upsert);
			await context.SaveChangesAsync(ct);
			return;
		}

		var entity = new DiscoveredChannel
		{
			Id = guidFactory.New(),
			Username = upsert.Username,
			TgUrl = upsert.TgUrl,
			LastParsedId = upsert.LastParsedId,
			TelegramId = upsert.TelegramId,
			PeerType = upsert.PeerType,
			Title = upsert.Title,
			Description = upsert.Description,
			AvatarUrl = upsert.AvatarUrl,
			ParticipantsCount = upsert.ParticipantsCount,
			InviteHash = upsert.InviteHash,
			DiscoveredFromChannelId = upsert.DiscoveredFromChannelId,
			Status = DiscoveryStatus.Pending
		};

		context.DiscoveredChannels.Add(entity);
		await context.SaveChangesAsync(ct);
	}

	public async Task ChannelBanned(Guid id, CancellationToken ct)
	{
		var entity = await context.DiscoveredChannels.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
		entity!.IsBanned = true;
		await context.SaveChangesAsync(ct);
	}

	public async Task MarkAsSkippedAsync(Guid id, CancellationToken ct)
	{
		var entity = await context.DiscoveredChannels.FirstAsync(x => x.Id == id, ct);
		entity.Status = DiscoveryStatus.Skipped;
		entity.LastDiscoveredAt = DateTimeOffset.UtcNow;
		await context.SaveChangesAsync(ct);
	}

	public async Task BulkUpsertAsync(IReadOnlyCollection<DiscoveredPeerUpsert> upserts, CancellationToken ct)
	{
		if (upserts.Count == 0)
			return;

		var usernames = upserts
			.Where(x => x.Username is not null)
			.Select(x => x.Username!)
			.Distinct()
			.ToArray();

		var telegramIds = upserts
			.Where(x => x.TelegramId is not null)
			.Select(x => x.TelegramId!.Value)
			.Distinct()
			.ToArray();

		var inviteHashes = upserts
			.Where(x => x.InviteHash is not null)
			.Select(x => x.InviteHash!)
			.Distinct()
			.ToArray();

		var existing = await context.DiscoveredChannels
			.Where(x =>
				(x.Username != null && usernames.Contains(x.Username))
				|| (x.TelegramId != null && telegramIds.Contains(x.TelegramId.Value))
				|| (x.InviteHash != null && inviteHashes.Contains(x.InviteHash)))
			.ToListAsync(ct);

		var byUsername = existing
			.Where(x => x.Username is not null)
			.ToDictionary(x => x.Username!, StringComparer.OrdinalIgnoreCase);
		var byTelegramId = existing
			.Where(x => x.TelegramId is not null)
			.ToDictionary(x => x.TelegramId!.Value);
		var byInviteHash = existing
			.Where(x => x.InviteHash is not null)
			.ToDictionary(x => x.InviteHash!, StringComparer.OrdinalIgnoreCase);

		foreach (var upsert in upserts)
		{
			DiscoveredChannel? match = null;
			if (upsert.TelegramId is not null && byTelegramId.TryGetValue(upsert.TelegramId.Value, out var byTid))
				match = byTid;
			else if (upsert.Username is not null && byUsername.TryGetValue(upsert.Username, out var byUn))
				match = byUn;
			else if (upsert.InviteHash is not null && byInviteHash.TryGetValue(upsert.InviteHash, out var byIh))
				match = byIh;

			if (match is not null)
			{
				ApplyUpsertFields(match, upsert);
				if (match.Username is not null)
					byUsername[match.Username] = match;
				if (match.TelegramId is not null)
					byTelegramId[match.TelegramId.Value] = match;
				if (match.InviteHash is not null)
					byInviteHash[match.InviteHash] = match;
			}
			else
			{
				var entity = new DiscoveredChannel
				{
					Id = guidFactory.New(),
					Username = upsert.Username,
					TgUrl = upsert.TgUrl,
					LastParsedId = upsert.LastParsedId,
					TelegramId = upsert.TelegramId,
					PeerType = upsert.PeerType,
					Title = upsert.Title,
					Description = upsert.Description,
					AvatarUrl = upsert.AvatarUrl,
					ParticipantsCount = upsert.ParticipantsCount,
					InviteHash = upsert.InviteHash,
					DiscoveredFromChannelId = upsert.DiscoveredFromChannelId,
					Status = DiscoveryStatus.Pending
				};
				context.DiscoveredChannels.Add(entity);

				if (entity.Username is not null)
					byUsername[entity.Username] = entity;
				if (entity.TelegramId is not null)
					byTelegramId[entity.TelegramId.Value] = entity;
				if (entity.InviteHash is not null)
					byInviteHash[entity.InviteHash] = entity;
			}
		}

		await context.SaveChangesAsync(ct);
	}

	private Task<DiscoveredChannel?> FindExistingAsync(
		string? username,
		long? telegramId,
		string? inviteHash,
		CancellationToken ct
	)
	{
		return context.DiscoveredChannels
			.FirstOrDefaultAsync(x =>
				(telegramId != null && x.TelegramId == telegramId)
				|| (username != null && x.Username == username)
				|| (inviteHash != null && x.InviteHash == inviteHash), ct);
	}

	private static void ApplyUpsertFields(DiscoveredChannel existing, DiscoveredPeerUpsert upsert)
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
		if (upsert.Description is not null)
			existing.Description = upsert.Description;
		if (upsert.AvatarUrl is not null)
			existing.AvatarUrl = upsert.AvatarUrl;
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
}
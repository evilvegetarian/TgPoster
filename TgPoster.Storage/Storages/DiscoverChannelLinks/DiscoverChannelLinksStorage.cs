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
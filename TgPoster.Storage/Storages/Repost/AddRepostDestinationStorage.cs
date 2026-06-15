using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Repost.AddRepostDestination;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class AddRepostDestinationStorage(PosterContext context, GuidFactory guidFactory) : IAddRepostDestinationStorage
{
	public Task<Guid?> GetTelegramSessionIdAsync(Guid repostSettingsId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.Where(x => x.Id == repostSettingsId)
			.Select(x => (Guid?)x.TelegramSessionId)
			.FirstOrDefaultAsync(ct);
	}

	public Task<bool> DestinationExistsAsync(Guid repostSettingsId, long chatIdentifier, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.AnyAsync(x => x.RepostSettingsId == repostSettingsId && x.ChatId == chatIdentifier, ct);
	}

	public async Task<Guid> UpsertDiscoveredChannelAsync(
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		bool canSendMessages,
		bool canSendMedia,
		CancellationToken ct)
	{
		var normalizedUsername = string.IsNullOrWhiteSpace(username) ? null : username;
		var peerType = chatType switch
		{
			ChatType.Channel => "channel",
			ChatType.Group => "chat",
			_ => null
		};

		// IgnoreQueryFilters: уникальные индексы по Username покрывают и забаненные строки —
		// без этого можно не увидеть существующую запись и словить duplicate key
		var existing = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.Where(x => x.TelegramId == chatId
			            || (normalizedUsername != null && x.Username == normalizedUsername))
			.FirstOrDefaultAsync(ct);

		if (existing != null)
		{
			// Обновляем тем, что собираем сейчас — только непустыми значениями
			if (title != null)
			{
				existing.Title = title;
			}

			if (normalizedUsername != null)
			{
				existing.Username = normalizedUsername;
			}

			existing.TelegramId = chatId;

			if (memberCount != null)
			{
				existing.ParticipantsCount = memberCount;
			}

			if (peerType != null)
			{
				existing.PeerType = peerType;
			}

			existing.CanSendMessages = canSendMessages;
			existing.CanSendMedia = canSendMedia;

			await context.SaveChangesAsync(ct);
			return existing.Id;
		}

		var discovered = new DiscoveredChannel
		{
			Id = guidFactory.New(),
			Username = normalizedUsername,
			Title = title,
			TelegramId = chatId,
			ParticipantsCount = memberCount,
			PeerType = peerType,
			TgUrl = normalizedUsername != null ? $"https://t.me/{normalizedUsername}" : null,
			CanSendMessages = canSendMessages,
			CanSendMedia = canSendMedia,
			Status = DiscoveryStatus.Pending
		};

		await context.DiscoveredChannels.AddAsync(discovered, ct);
		await context.SaveChangesAsync(ct);

		return discovered.Id;
	}

	public async Task<Guid> AddDestinationAsync(
		Guid repostSettingsId,
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		ChatStatus chatStatus,
		string? avatarBase64,
		Guid discoveredChannelId,
		CancellationToken ct)
	{
		var destination = new RepostDestination
		{
			Id = guidFactory.New(),
			RepostSettingsId = repostSettingsId,
			ChatId = chatId,
			IsActive = true,
			Title = title,
			Username = username,
			MemberCount = memberCount,
			ChatType = chatType,
			ChatStatus = chatStatus,
			AvatarBase64 = avatarBase64,
			InfoUpdatedAt = DateTimeOffset.UtcNow,
			DiscoveredChannelId = discoveredChannelId
		};

		await context.AddAsync(destination, ct);
		await context.SaveChangesAsync(ct);

		return destination.Id;
	}
}

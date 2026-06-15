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

	public async Task<AddDestinationResult> AddDestinationAsync(
		Guid repostSettingsId,
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		ChatStatus chatStatus,
		string? avatarBase64,
		CancellationToken ct)
	{
		var discoveredChannelId = await FindOrCreateDiscoveredChannelAsync(
			chatId, title, username, memberCount, chatType, ct);

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

		return new AddDestinationResult(destination.Id, discoveredChannelId);
	}

	/// <summary>
	///     Ищет запись в Discover по TelegramId/username. Если нашлась — обновляет её свежей инфой,
	///     иначе создаёт новую. Возвращает Id записи Discover.
	/// </summary>
	private async Task<Guid> FindOrCreateDiscoveredChannelAsync(
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
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
			Status = DiscoveryStatus.Pending
		};

		await context.DiscoveredChannels.AddAsync(discovered, ct);

		return discovered.Id;
	}
}

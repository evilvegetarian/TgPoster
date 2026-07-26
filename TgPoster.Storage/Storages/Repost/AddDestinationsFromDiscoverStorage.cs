using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class AddDestinationsFromDiscoverStorage(PosterContext context, GuidFactory guidFactory)
	: IAddDestinationsFromDiscoverStorage
{
	public Task<RepostSettingsDefaults?> GetSettingsDefaultsAsync(
		Guid repostSettingsId,
		Guid userId,
		CancellationToken ct
	)
	{
		return context.Set<RepostSettings>()
			.Where(x => x.Id == repostSettingsId && x.Schedule.UserId == userId)
			.Select(x => new RepostSettingsDefaults(
				x.TelegramSessionId,
				x.Schedule.ChannelId,
				x.DefaultDelayMinSeconds,
				x.DefaultDelayMaxSeconds,
				x.DefaultRepostEveryNth,
				x.DefaultSkipProbability,
				x.DefaultMaxRepostsPerDay))
			.FirstOrDefaultAsync(ct);
	}

	public Task<List<DiscoverCandidate>> GetCandidatesAsync(
		IReadOnlyList<Guid> discoveredChannelIds,
		CancellationToken ct
	)
	{
		return context.DiscoveredChannels
			.Where(x => discoveredChannelIds.Contains(x.Id))
			.Select(x => new DiscoverCandidate(
				x.Id,
				x.TelegramId,
				x.Username,
				x.Title,
				x.InviteHash,
				x.ParticipantsCount))
			.ToListAsync(ct);
	}

	public Task<List<long>> GetExistingChatIdsAsync(Guid repostSettingsId, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.Where(x => x.RepostSettingsId == repostSettingsId)
			.Select(x => x.ChatId)
			.ToListAsync(ct);
	}

	public async Task UpdateDiscoveredChannelAsync(
		Guid discoveredChannelId,
		long telegramId,
		string? title,
		string? username,
		ChatType chatType,
		bool canSendMessages,
		bool canSendMedia,
		CancellationToken ct
	)
	{
		var channel = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.FirstAsync(x => x.Id == discoveredChannelId, ct);

		var normalizedUsername = string.IsNullOrWhiteSpace(username) ? null : username;

		channel.TelegramId = telegramId;

		if (title != null)
		{
			channel.Title = title;
		}

		if (normalizedUsername != null)
		{
			channel.Username = normalizedUsername;
			channel.TgUrl ??= $"https://t.me/{normalizedUsername}";
		}

		var peerType = chatType switch
		{
			ChatType.Channel => "channel",
			ChatType.Group => "chat",
			_ => null
		};

		if (peerType != null)
		{
			channel.PeerType = peerType;
		}

		channel.CanSendMessages = canSendMessages;
		channel.CanSendMedia = canSendMedia;

		await context.SaveChangesAsync(ct);
	}

	public async Task<Guid> AddDestinationAsync(
		Guid repostSettingsId,
		long chatId,
		string? title,
		string? username,
		int? memberCount,
		ChatType chatType,
		Guid discoveredChannelId,
		int delayMinSeconds,
		int delayMaxSeconds,
		int repostEveryNth,
		int skipProbability,
		int? maxRepostsPerDay,
		CancellationToken ct
	)
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
			ChatStatus = ChatStatus.Active,
			InfoUpdatedAt = DateTimeOffset.UtcNow,
			DiscoveredChannelId = discoveredChannelId,
			DelayMinSeconds = delayMinSeconds,
			DelayMaxSeconds = delayMaxSeconds,
			RepostEveryNth = repostEveryNth,
			SkipProbability = skipProbability,
			MaxRepostsPerDay = maxRepostsPerDay
		};

		await context.AddAsync(destination, ct);
		await context.SaveChangesAsync(ct);

		return destination.Id;
	}
}

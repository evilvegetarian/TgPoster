using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class GetRepostSettingsStorage(PosterContext context) : IGetRepostSettingsStorage
{
	public async Task<RepostSettingsResponse?> GetAsync(Guid id, Guid userId, CancellationToken ct)
	{
		// Сутки те же, что у воркера при проверке дневного лимита
		var todayUtc = DateTime.UtcNow.Date;

		var settings = await context.Set<RepostSettings>()
			.Where(x => x.Id == id && x.Schedule.UserId == userId)
			.Select(x => new RepostSettingsResponse
			{
				Id = x.Id,
				ScheduleId = x.ScheduleId,
				ScheduleName = x.Schedule.Name,
				TelegramSessionId = x.TelegramSessionId,
				TelegramSessionName = x.TelegramSession.Name,
				IsActive = x.IsActive,
				DefaultDelayMinSeconds = x.DefaultDelayMinSeconds,
				DefaultDelayMaxSeconds = x.DefaultDelayMaxSeconds,
				DefaultRepostEveryNth = x.DefaultRepostEveryNth,
				DefaultSkipProbability = x.DefaultSkipProbability,
				DefaultMaxRepostsPerDay = x.DefaultMaxRepostsPerDay,
				Created = x.Created!.Value,
				Destinations = x.Destinations.Select(d => new RepostDestinationDto
				{
					Id = d.Id,
					ChatId = d.ChatId,
					IsActive = d.IsActive,
					Title = d.Title,
					Username = d.Username,
					MemberCount = d.MemberCount,
					ChatType = d.ChatType,
					// Бан хранится только в реестре банов аккаунта, в направлении — лишь членство в чате
					ChatStatus = context.TelegramSessionChatBans
						.Any(b => b.TelegramSessionId == x.TelegramSessionId && b.ChatId == d.ChatId)
						? ChatStatus.Banned
						: d.ChatStatus,
					AvatarBase64 = d.AvatarBase64,
					InfoUpdatedAt = d.InfoUpdatedAt,
					DiscoveredChannelId = d.DiscoveredChannelId,
					DelayMinSeconds = d.DelayMinSeconds,
					DelayMaxSeconds = d.DelayMaxSeconds,
					RepostEveryNth = d.RepostEveryNth,
					SkipProbability = d.SkipProbability,
					MaxRepostsPerDay = d.MaxRepostsPerDay,
					LastRepostAt = d.RepostLogs
						.Where(l => l.Status == RepostStatus.Success)
						.Max(l => l.RepostedAt),
					RepostsToday = d.RepostLogs.Count(l => l.Status == RepostStatus.Success
					                                       && l.RepostedAt != null
					                                       && l.RepostedAt.Value >= todayUtc),
					BanError = context.TelegramSessionChatBans
						.Where(b => b.TelegramSessionId == x.TelegramSessionId && b.ChatId == d.ChatId)
						.OrderByDescending(b => b.DetectedAt)
						.Select(b => b.Error)
						.FirstOrDefault(),
					BannedAt = context.TelegramSessionChatBans
						.Where(b => b.TelegramSessionId == x.TelegramSessionId && b.ChatId == d.ChatId)
						.Max(b => (DateTimeOffset?)b.DetectedAt)
				}).ToList(),
				RemovedDestinations = new List<RepostDestinationDto>()
			})
			.FirstOrDefaultAsync(ct);

		if (settings is null)
		{
			return null;
		}

		return settings with { RemovedDestinations = await GetRemovedDestinationsAsync(settings, ct) };
	}

	/// <summary>
	/// Удалённые, но не стёртые насовсем каналы настроек. По каждому чату — только последнее удаление,
	/// и только если этот чат не добавлен в список заново
	/// </summary>
	/// <param name="settings"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task<List<RepostDestinationDto>> GetRemovedDestinationsAsync(
		RepostSettingsResponse settings,
		CancellationToken ct
	)
	{
		var liveChatIds = settings.Destinations.Select(d => d.ChatId).ToList();
		var sessionId = settings.TelegramSessionId;

		// Фильтры мягкого удаления сняты для всего запроса, поэтому у реестра банов Deleted проверяется явно
		var removed = await context.Set<RepostDestination>()
			.IgnoreQueryFilters()
			.Where(d => d.RepostSettingsId == settings.Id
			            && d.Deleted != null
			            && d.PurgedAt == null
			            && !liveChatIds.Contains(d.ChatId))
			.OrderByDescending(d => d.Deleted)
			.Select(d => new RepostDestinationDto
			{
				Id = d.Id,
				ChatId = d.ChatId,
				IsActive = false,
				Title = d.Title,
				Username = d.Username,
				MemberCount = d.MemberCount,
				ChatType = d.ChatType,
				ChatStatus = context.TelegramSessionChatBans
					.Any(b => b.TelegramSessionId == sessionId && b.ChatId == d.ChatId && b.Deleted == null)
					? ChatStatus.Banned
					: d.ChatStatus,
				AvatarBase64 = d.AvatarBase64,
				InfoUpdatedAt = d.InfoUpdatedAt,
				DiscoveredChannelId = d.DiscoveredChannelId,
				DelayMinSeconds = d.DelayMinSeconds,
				DelayMaxSeconds = d.DelayMaxSeconds,
				RepostEveryNth = d.RepostEveryNth,
				SkipProbability = d.SkipProbability,
				MaxRepostsPerDay = d.MaxRepostsPerDay,
				LastRepostAt = d.RepostLogs
					.Where(l => l.Status == RepostStatus.Success)
					.Max(l => l.RepostedAt),
				RepostsToday = 0,
				BanError = context.TelegramSessionChatBans
					.Where(b => b.TelegramSessionId == sessionId && b.ChatId == d.ChatId && b.Deleted == null)
					.OrderByDescending(b => b.DetectedAt)
					.Select(b => b.Error)
					.FirstOrDefault(),
				BannedAt = context.TelegramSessionChatBans
					.Where(b => b.TelegramSessionId == sessionId && b.ChatId == d.ChatId && b.Deleted == null)
					.Max(b => (DateTimeOffset?)b.DetectedAt),
				DeletedAt = d.Deleted
			})
			.ToListAsync(ct);

		return removed
			.DistinctBy(d => d.ChatId)
			.ToList();
	}
}

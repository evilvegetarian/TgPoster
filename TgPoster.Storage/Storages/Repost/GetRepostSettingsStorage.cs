using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class GetRepostSettingsStorage(PosterContext context) : IGetRepostSettingsStorage
{
	public Task<RepostSettingsResponse?> GetAsync(Guid id, Guid userId, CancellationToken ct)
	{
		// Сутки те же, что у воркера при проверке дневного лимита
		var todayUtc = DateTime.UtcNow.Date;

		return context.Set<RepostSettings>()
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
					                                       && l.RepostedAt.Value >= todayUtc)
				}).ToList()
			})
			.FirstOrDefaultAsync(ct);
	}
}
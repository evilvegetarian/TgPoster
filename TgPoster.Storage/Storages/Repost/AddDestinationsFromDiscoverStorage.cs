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
				x.TelegramSession.FloodWaitUntil))
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
				x.CanSendMessages,
				x.CanSendMedia))
			.ToListAsync(ct);
	}

	public Task<List<DiscoverCandidate>> GetCandidatesByFilterAsync(
		DiscoverImportFilter filter,
		IReadOnlyCollection<long> excludedChatIds,
		int limit,
		CancellationToken ct
	)
	{
		return context.DiscoveredChannels
			.ApplyDiscoverFilter(
				filter.Category,
				filter.PeerType,
				filter.Search,
				filter.MinParticipants,
				filter.MaxParticipants)
			// Уже добавленные каналы и канал-источник заняли бы места в задании впустую
			.Where(x => x.TelegramId == null || !excludedChatIds.Contains(x.TelegramId.Value))
			// Права из прошлых проверок: писать нельзя — резолвить и вступать незачем
			.Where(x => x.CanSendMessages != false && x.CanSendMedia != false)
			// Канал без единого идентификатора в Telegram не открыть
			.Where(x => x.Username != null || x.InviteHash != null || x.TelegramId != null)
			.ApplyDiscoverSort(filter.SortBy, filter.SortDirection)
			.Take(limit)
			.Select(x => new DiscoverCandidate(
				x.Id,
				x.TelegramId,
				x.Username,
				x.Title,
				x.InviteHash,
				x.CanSendMessages,
				x.CanSendMedia))
			.ToListAsync(ct);
	}

	public Task<List<long>> GetExistingChatIdsAsync(Guid repostSettingsId, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.Where(x => x.RepostSettingsId == repostSettingsId)
			.Select(x => x.ChatId)
			.ToListAsync(ct);
	}

	public async Task<Guid?> GetActiveJobIdAsync(Guid repostSettingsId, CancellationToken ct)
	{
		var jobId = await context.RepostImportJobs
			.Where(x => x.RepostSettingsId == repostSettingsId)
			.Where(x => x.Status == RepostImportStatus.Pending
			            || x.Status == RepostImportStatus.InProgress
			            || x.Status == RepostImportStatus.CooldownWait)
			.Select(x => (Guid?)x.Id)
			.FirstOrDefaultAsync(ct);

		return jobId;
	}

	public async Task<Guid> CreateImportJobAsync(
		Guid repostSettingsId,
		bool autoJoin,
		IReadOnlyList<ImportJobItemDto> items,
		CancellationToken ct
	)
	{
		var now = DateTimeOffset.UtcNow;

		var job = new RepostImportJob
		{
			Id = guidFactory.New(),
			RepostSettingsId = repostSettingsId,
			AutoJoin = autoJoin,
			Status = RepostImportStatus.Pending
		};

		job.Items = items
			.Select((x, index) => new RepostImportJobItem
			{
				Id = guidFactory.New(),
				RepostImportJobId = job.Id,
				DiscoveredChannelId = x.DiscoveredChannelId,
				Title = x.Title,
				Order = index,
				Outcome = x.Outcome,
				Error = x.Error,
				ProcessedAt = x.Outcome == AddDestinationOutcome.Pending ? null : now
			})
			.ToList();

		await context.AddAsync(job, ct);
		await context.SaveChangesAsync(ct);

		return job.Id;
	}
}

using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class ImportRepostDestinationsStorage(PosterContext context, GuidFactory guidFactory)
	: IImportRepostDestinationsStorage
{
	/// <summary>
	///     Ограничение колонки Error в RepostImportJobItems
	/// </summary>
	private const int MaxErrorLength = 1000;

	public Task<ImportJobData?> GetJobAsync(Guid jobId, CancellationToken ct)
	{
		return context.RepostImportJobs
			.Where(x => x.Id == jobId)
			.Select(x => new ImportJobData(
				x.Id,
				x.RepostSettingsId,
				x.RepostSettings.TelegramSessionId,
				x.RepostSettings.Schedule.ChannelId,
				x.RepostSettings.TelegramSession.FloodWaitUntil,
				x.AutoJoin,
				x.Status,
				x.RepostSettings.DefaultDelayMinSeconds,
				x.RepostSettings.DefaultDelayMaxSeconds,
				x.RepostSettings.DefaultRepostEveryNth,
				x.RepostSettings.DefaultSkipProbability,
				x.RepostSettings.DefaultMaxRepostsPerDay))
			.FirstOrDefaultAsync(ct);
	}

	public Task<List<ImportJobPendingItem>> GetPendingItemsAsync(Guid jobId, CancellationToken ct)
	{
		// Данные канала берём из Discover: пока задание стояло в очереди, они могли обновиться
		return context.RepostImportJobItems
			.Where(x => x.RepostImportJobId == jobId && x.Outcome == AddDestinationOutcome.Pending)
			.OrderBy(x => x.Order)
			.Join(
				context.DiscoveredChannels,
				item => item.DiscoveredChannelId,
				channel => channel.Id,
				(item, channel) => new ImportJobPendingItem(
					item.Id,
					item.DiscoveredChannelId,
					channel.TelegramId,
					channel.Username,
					channel.InviteHash,
					channel.ParticipantsCount,
					channel.CanSendMessages,
					channel.CanSendMedia))
			.ToListAsync(ct);
	}

	public Task<List<long>> GetExistingChatIdsAsync(Guid repostSettingsId, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.Where(x => x.RepostSettingsId == repostSettingsId)
			.Select(x => x.ChatId)
			.ToListAsync(ct);
	}

	public async Task UpdateItemAsync(
		Guid itemId,
		AddDestinationOutcome outcome,
		Guid? repostDestinationId,
		string? error,
		CancellationToken ct
	)
	{
		var item = await context.RepostImportJobItems.FirstAsync(x => x.Id == itemId, ct);

		item.Outcome = outcome;
		item.RepostDestinationId = repostDestinationId;
		item.Error = Truncate(error);
		item.ProcessedAt = DateTimeOffset.UtcNow;

		await context.SaveChangesAsync(ct);
	}

	public async Task SetJobStatusAsync(
		Guid jobId,
		RepostImportStatus status,
		string? error,
		CancellationToken ct
	)
	{
		var job = await context.RepostImportJobs.FirstAsync(x => x.Id == jobId, ct);

		job.Status = status;
		job.LastError = Truncate(error);

		if (status == RepostImportStatus.InProgress)
		{
			job.StartedAt ??= DateTimeOffset.UtcNow;
		}

		if (status is RepostImportStatus.Completed or RepostImportStatus.Failed)
		{
			job.CompletedAt = DateTimeOffset.UtcNow;
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task SetSessionFloodWaitAsync(
		Guid telegramSessionId,
		DateTimeOffset floodWaitUntil,
		CancellationToken ct
	)
	{
		var session = await context.TelegramSessions.FirstAsync(x => x.Id == telegramSessionId, ct);

		session.FloodWaitUntil = floodWaitUntil;

		await context.SaveChangesAsync(ct);
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

	private static string? Truncate(string? value) =>
		value is { Length: > MaxErrorLength } ? value[..MaxErrorLength] : value;
}

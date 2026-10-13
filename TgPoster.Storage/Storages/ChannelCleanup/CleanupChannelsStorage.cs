using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.UseCases.CleanupChannels;

namespace TgPoster.Storage.Storages.ChannelCleanup;

internal sealed class CleanupChannelsStorage(PosterContext context) : ICleanupChannelsStorage
{
	/// <summary>
	/// Ограничение колонок Error и LastError
	/// </summary>
	private const int MaxErrorLength = 1000;

	public Task<CleanupJobData?> GetJobAsync(Guid jobId, CancellationToken ct)
	{
		return context.ChannelCleanupJobs
			.Where(x => x.Id == jobId)
			.Select(x => new CleanupJobData(
				x.Id,
				x.TelegramSessionId,
				x.TelegramSession.FloodWaitUntil,
				x.Status))
			.FirstOrDefaultAsync(ct);
	}

	public Task<List<CleanupJobPendingItem>> GetPendingItemsAsync(Guid jobId, CancellationToken ct)
	{
		return context.ChannelCleanupJobItems
			.Where(x => x.ChannelCleanupJobId == jobId && x.Outcome == ChannelCleanupOutcome.Pending)
			.OrderBy(x => x.Order)
			.Select(x => new CleanupJobPendingItem(x.Id, x.ChatId, x.AccessHash, x.IsBasicGroup, x.Username))
			.ToListAsync(ct);
	}

	public Task<UsedTelegramChats> GetUsedChatsAsync(Guid sessionId, CancellationToken ct)
	{
		return UsedTelegramChatsQuery.LoadAsync(context, sessionId, ct);
	}

	public async Task UpdateItemAsync(Guid itemId, ChannelCleanupOutcome outcome, string? error, CancellationToken ct)
	{
		var item = await context.ChannelCleanupJobItems.FirstAsync(x => x.Id == itemId, ct);

		item.Outcome = outcome;
		item.Error = Truncate(error);
		item.ProcessedAt = DateTimeOffset.UtcNow;

		await context.SaveChangesAsync(ct);
	}

	public async Task MarkPendingItemsNotProcessedAsync(Guid jobId, string error, CancellationToken ct)
	{
		var items = await context.ChannelCleanupJobItems
			.Where(x => x.ChannelCleanupJobId == jobId && x.Outcome == ChannelCleanupOutcome.Pending)
			.ToListAsync(ct);

		var now = DateTimeOffset.UtcNow;
		var truncatedError = Truncate(error);

		foreach (var item in items)
		{
			item.Outcome = ChannelCleanupOutcome.NotProcessed;
			item.Error = truncatedError;
			item.ProcessedAt = now;
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task SetJobStatusAsync(
		Guid jobId,
		ChannelCleanupStatus status,
		string? error,
		CancellationToken ct
	)
	{
		var job = await context.ChannelCleanupJobs.FirstAsync(x => x.Id == jobId, ct);

		job.Status = status;
		job.LastError = Truncate(error);

		if (status == ChannelCleanupStatus.InProgress)
		{
			// Отметку обновляем каждым проходом: по ней определяется, что задание зависло
			job.StartedAt = DateTimeOffset.UtcNow;
		}

		if (status is ChannelCleanupStatus.Completed or ChannelCleanupStatus.Failed)
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

	private static string? Truncate(string? value) =>
		value is { Length: > MaxErrorLength } ? value[..MaxErrorLength] : value;
}

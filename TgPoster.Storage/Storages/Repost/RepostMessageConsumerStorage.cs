using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class RepostMessageConsumerStorage(PosterContext context, GuidFactory guidFactory)
	: IRepostMessageConsumerStorage
{
	private const int MaxErrorLength = 2000;

	public Task<RepostDataDto?> GetRepostDataAsync(Guid messageId, Guid repostSettingsId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.Where(rs => rs.Id == repostSettingsId && rs.IsActive)
			.Select(rs => new RepostDataDto
			{
				TelegramMessageId = rs.Schedule.Messages
					.Where(m => m.Id == messageId)
					.Select(m => m.TelegramMessageId)
					.FirstOrDefault(),
				TelegramSessionId = rs.TelegramSessionId,
				SourceChannelIdentifier = rs.Schedule.ChannelName,
				Destinations = rs.Destinations
					.Where(d => d.IsActive)
					.Select(d => new RepostDestinationDataDto
					{
						Id = d.Id,
						ChatIdentifier = d.ChatId,
						DelayMinSeconds = d.DelayMinSeconds,
						DelayMaxSeconds = d.DelayMaxSeconds,
						RepostEveryNth = d.RepostEveryNth,
						SkipProbability = d.SkipProbability,
						MaxRepostsPerDay = d.MaxRepostsPerDay
					}).ToList()
			})
			.FirstOrDefaultAsync(ct);
	}

	public async Task CreateRepostLogsAsync(IReadOnlyCollection<RepostLogEntry> entries, CancellationToken ct)
	{
		if (entries.Count == 0)
		{
			return;
		}

		var logs = entries.Select(entry => new RepostLog
		{
			Id = guidFactory.New(),
			MessageId = entry.MessageId,
			RepostDestinationId = entry.RepostDestinationId,
			TelegramMessageId = entry.TelegramMessageId,
			Status = entry.Status,
			Reason = entry.Reason,
			RepostedAt = entry.Status == RepostStatus.Success ? DateTime.UtcNow : null,
			Error = Truncate(entry.Error)
		});

		await context.Set<RepostLog>().AddRangeAsync(logs, ct);
		await context.SaveChangesAsync(ct);
	}

	public async Task UpdateDestinationStatusAsync(
		Guid destinationId,
		ChatStatus chatStatus,
		bool isActive,
		CancellationToken ct
	)
	{
		var destination = await context.Set<RepostDestination>()
			.FirstAsync(x => x.Id == destinationId, ct);

		destination.ChatStatus = chatStatus;
		destination.IsActive = isActive;
		destination.InfoUpdatedAt = DateTimeOffset.UtcNow;

		await context.SaveChangesAsync(ct);
	}

	public async Task<int> IncrementRepostCounterAsync(Guid destinationId, CancellationToken ct)
	{
		var destination = await context.Set<RepostDestination>()
			.FirstAsync(x => x.Id == destinationId, ct);

		destination.RepostCounter++;
		await context.SaveChangesAsync(ct);

		return destination.RepostCounter;
	}

	public Task<int> GetTodayRepostCountAsync(Guid destinationId, CancellationToken ct)
	{
		var todayUtc = DateTime.UtcNow.Date;

		return context.Set<RepostLog>()
			.Where(l => l.RepostDestinationId == destinationId
			            && l.Status == RepostStatus.Success
			            && l.RepostedAt != null
			            && l.RepostedAt.Value >= todayUtc)
			.CountAsync(ct);
	}

	private static string? Truncate(string? error) =>
		error is { Length: > MaxErrorLength } ? error[..MaxErrorLength] : error;
}

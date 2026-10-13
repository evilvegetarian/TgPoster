using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.ChannelCleanup.StartChannelCleanup;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Telegram.Models;

namespace TgPoster.Storage.Storages.ChannelCleanup;

internal sealed class StartChannelCleanupStorage(PosterContext context, GuidFactory guidFactory)
	: IStartChannelCleanupStorage
{
	public Task<bool> SessionExistsAsync(Guid sessionId, Guid userId, CancellationToken ct)
	{
		return context.TelegramSessions.AnyAsync(x => x.Id == sessionId && x.UserId == userId, ct);
	}

	public Task<UsedTelegramChats> GetUsedChatsAsync(Guid sessionId, CancellationToken ct)
	{
		return UsedTelegramChatsQuery.LoadAsync(context, sessionId, ct);
	}

	public Task<Guid?> GetActiveJobIdAsync(Guid sessionId, CancellationToken ct)
	{
		// Без Pending-каналов задание уже ничего не сделает, даже если статус не успел смениться
		return context.ChannelCleanupJobs
			.Where(x => x.TelegramSessionId == sessionId
			            && (x.Status == ChannelCleanupStatus.Pending
			                || x.Status == ChannelCleanupStatus.InProgress
			                || x.Status == ChannelCleanupStatus.CooldownWait)
			            && x.Items.Any(i => i.Outcome == ChannelCleanupOutcome.Pending))
			.Select(x => (Guid?)x.Id)
			.FirstOrDefaultAsync(ct);
	}

	public async Task<Guid> CreateJobAsync(
		Guid sessionId,
		IReadOnlyList<ChannelCleanupItemDraft> items,
		CancellationToken ct
	)
	{
		var jobId = guidFactory.New();
		var job = new ChannelCleanupJob
		{
			Id = jobId,
			TelegramSessionId = sessionId,
			Status = ChannelCleanupStatus.Pending,
			Items = items
				.Select((x, index) => new ChannelCleanupJobItem
				{
					Id = guidFactory.New(),
					ChannelCleanupJobId = jobId,
					ChatId = x.ChatId,
					AccessHash = x.AccessHash,
					IsBasicGroup = x.IsBasicGroup,
					ChatType = x.ChatType,
					Title = x.Title,
					Username = x.Username,
					Order = index,
					Outcome = ChannelCleanupOutcome.Pending
				})
				.ToList()
		};

		await context.ChannelCleanupJobs.AddAsync(job, ct);
		await context.SaveChangesAsync(ct);

		return job.Id;
	}
}

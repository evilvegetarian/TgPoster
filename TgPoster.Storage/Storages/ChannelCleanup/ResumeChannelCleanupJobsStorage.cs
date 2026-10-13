using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Worker.Domain.UseCases.CleanupChannels;

namespace TgPoster.Storage.Storages.ChannelCleanup;

internal sealed class ResumeChannelCleanupJobsStorage(PosterContext context) : IResumeChannelCleanupJobsStorage
{
	/// <summary>
	/// Через сколько задание в статусе Pending считается потерянным: publish из API не дошёл
	/// </summary>
	private static readonly TimeSpan LostAfter = TimeSpan.FromMinutes(2);

	/// <summary>
	/// Через сколько задание в статусе InProgress считается зависшим: воркер упал посреди обработки
	/// </summary>
	private static readonly TimeSpan StuckAfter = TimeSpan.FromMinutes(30);

	public Task<List<Guid>> GetResumableJobIdsAsync(CancellationToken ct)
	{
		var now = DateTimeOffset.UtcNow;
		var lostBefore = now - LostAfter;
		var stuckBefore = now - StuckAfter;

		return context.ChannelCleanupJobs
			.Where(x => x.TelegramSession.FloodWaitUntil == null || x.TelegramSession.FloodWaitUntil <= now)
			.Where(x => x.Status == ChannelCleanupStatus.CooldownWait
			            || (x.Status == ChannelCleanupStatus.Pending && x.Created <= lostBefore)
			            || (x.Status == ChannelCleanupStatus.InProgress && x.StartedAt <= stuckBefore))
			.Where(x => x.Items.Any(i => i.Outcome == ChannelCleanupOutcome.Pending))
			.OrderBy(x => x.Created)
			.Select(x => x.Id)
			.ToListAsync(ct);
	}
}

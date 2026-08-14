using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class ResumeRepostImportJobsStorage(PosterContext context) : IResumeRepostImportJobsStorage
{
	/// <summary>
	///     Через сколько задание в статусе Pending считается потерянным: publish из API не дошёл
	/// </summary>
	private static readonly TimeSpan LostAfter = TimeSpan.FromMinutes(2);

	/// <summary>
	///     Через сколько задание в статусе InProgress считается зависшим: воркер упал посреди обработки
	/// </summary>
	private static readonly TimeSpan StuckAfter = TimeSpan.FromMinutes(30);

	public Task<List<Guid>> GetResumableJobIdsAsync(CancellationToken ct)
	{
		var now = DateTimeOffset.UtcNow;
		var lostBefore = now - LostAfter;
		var stuckBefore = now - StuckAfter;

		return context.RepostImportJobs
			.Where(x => x.RepostSettings.TelegramSession.FloodWaitUntil == null
			            || x.RepostSettings.TelegramSession.FloodWaitUntil <= now)
			.Where(x => x.Status == RepostImportStatus.CooldownWait
			            || (x.Status == RepostImportStatus.Pending && x.Created <= lostBefore)
			            || (x.Status == RepostImportStatus.InProgress && x.StartedAt <= stuckBefore))
			.Where(x => x.Items.Any(i => i.Outcome == AddDestinationOutcome.Pending))
			.OrderBy(x => x.Created)
			.Select(x => x.Id)
			.ToListAsync(ct);
	}
}

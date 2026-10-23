using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;

namespace TgPoster.Worker.Domain.UseCases.ScheduleQueueCheck;

/// <summary>
/// Предупреждает владельца расписания, что запланированые посты кончились или скоро кончатся
/// </summary>
internal sealed class ScheduleQueueCheckWorker(
	IScheduleQueueCheckStorage storage,
	IScheduleAlertService alertService,
	TimeProvider timeProvider,
	ILogger<ScheduleQueueCheckWorker> logger,
	IHostApplicationLifetime lifetime)
{
	private static readonly TimeSpan RunningLowThreshold = TimeSpan.FromHours(48);

	public async Task CheckAsync()
	{
		var ct = lifetime.ApplicationStopping;
		var now = timeProvider.GetUtcNow();
		var queues = await storage.GetActiveSchedulesQueueAsync(now, ct);

		foreach (var queue in queues)
		{
			if (queue.PlannedCount == 0 || queue.LastPlannedAt is not { } lastPlannedAt)
			{
				await alertService.NotifyAsync(queue.ScheduleId, ScheduleProblem.QueueEmpty,
					WithAwaitingApproval("В очереди нет постов", queue.AwaitingApprovalCount), ct: ct);
				continue;
			}

			var remaining = lastPlannedAt - now;
			if (remaining >= RunningLowThreshold)
			{
				continue;
			}

			var details =
				$"Осталось постов: {queue.PlannedCount}, хватит примерно на {remaining.Days} д {remaining.Hours} ч";
			await alertService.NotifyAsync(queue.ScheduleId, ScheduleProblem.QueueRunningLow,
				WithAwaitingApproval(details, queue.AwaitingApprovalCount), ct: ct);
		}

		logger.LogDebug("Проверена очередь постов у {Count} расписаний", queues.Count);
	}

	/// <summary>
	/// Дописывает, сколько постов ждут подтверждения: они не уйдут, пока владелец их не подтвердит
	/// </summary>
	/// <param name="details"></param>
	/// <param name="awaitingApprovalCount"></param>
	/// <returns></returns>
	private static string WithAwaitingApproval(string details, int awaitingApprovalCount) =>
		awaitingApprovalCount > 0
			? $"{details}, ещё {awaitingApprovalCount} ждут подтверждения"
			: details;
}

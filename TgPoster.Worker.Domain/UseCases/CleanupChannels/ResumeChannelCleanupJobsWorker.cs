using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Telegram;

namespace TgPoster.Worker.Domain.UseCases.CleanupChannels;

/// <summary>
/// Возобновляет задания на очистку каналов: вставшие на паузу из-за ограничений Telegram
/// и зависшие после перезапуска воркера
/// </summary>
internal sealed class ResumeChannelCleanupJobsWorker(
	IResumeChannelCleanupJobsStorage storage,
	IBus bus,
	ILogger<ResumeChannelCleanupJobsWorker> logger,
	IHostApplicationLifetime lifetime)
{
	public async Task ResumeAsync()
	{
		var ct = lifetime.ApplicationStopping;

		var jobIds = await storage.GetResumableJobIdsAsync(ct);
		if (jobIds.Count == 0)
		{
			return;
		}

		logger.LogInformation("Возобновляем {Count} заданий на очистку каналов", jobIds.Count);

		foreach (var jobId in jobIds)
		{
			await bus.Publish(new CleanupChannelsContract { JobId = jobId }, ct);
		}
	}
}

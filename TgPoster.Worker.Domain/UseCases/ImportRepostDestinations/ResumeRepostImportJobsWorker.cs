using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Telegram;

namespace TgPoster.Worker.Domain.UseCases.ImportRepostDestinations;

/// <summary>
///     Возобновляет задания на массовое добавление каналов: те, что встали на паузу из-за
///     ограничений Telegram, и те, что зависли после перезапуска воркера.
/// </summary>
internal sealed class ResumeRepostImportJobsWorker(
	IResumeRepostImportJobsStorage storage,
	IBus bus,
	ILogger<ResumeRepostImportJobsWorker> logger,
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

		logger.LogInformation("Возобновляем {Count} заданий на добавление каналов", jobIds.Count);

		foreach (var jobId in jobIds)
		{
			await bus.Publish(new ImportRepostDestinationsContract { JobId = jobId }, ct);
		}
	}
}

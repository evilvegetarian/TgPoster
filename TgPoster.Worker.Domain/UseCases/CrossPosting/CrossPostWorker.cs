using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting;

/// <summary>
///     Recurring job кросс-постинга отправленных сообщений
/// </summary>
internal sealed class CrossPostWorker(
	ICrossPostWorkerStorage storage,
	ICrossPostPublisher publisher,
	TimeProvider timeProvider,
	ILogger<CrossPostWorker> logger,
	IHostApplicationLifetime lifetime)
{
	[AutomaticRetry(Attempts = 0)]
	[DisableConcurrentExecution(600)]
	public async Task ProcessAsync()
	{
		var ct = lifetime.ApplicationStopping;
		var start = timeProvider.GetUtcNow();

		await storage.EnqueueDueAsync(start, ct);
		await storage.FailStuckAsync(
			start.AddMinutes(-15),
			"Публикация прервана (перезапуск воркера) — проверьте пост в соцсети вручную",
			ct);
		await storage.SkipOrphanedAsync(ct);

		for (var i = 0; i < 10; i++)
		{
			if (timeProvider.GetUtcNow() - start >= TimeSpan.FromMinutes(4))
			{
				break;
			}

			var id = await storage.TakeNextDueAsync(timeProvider.GetUtcNow(), ct);
			if (id is null)
			{
				break;
			}

			try
			{
				await publisher.PublishAsync(id.Value, ct);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				logger.LogError(e, "Ошибка кросс-поста {CrossPostId}", id);
				await storage.MarkFailedAsync(id.Value, "Непредвиденная ошибка публикации", ct);
			}
		}
	}
}

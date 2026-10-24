using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

internal class ParseChannelWorker(
	IParseChannelWorkerStorage storage,
	ParseChannelUseCase parseChannelUseCase,
	IParseChannelRunStorage runStorage,
	IWorkerJobStatusStorage statusStorage,
	HangfireNextRunProvider nextRun,
	IScheduleAlertService alertService,
	TimeProvider timeProvider,
	ILogger<ParseChannelWorker> logger,
	IHostApplicationLifetime lifetime)
{
	/// <summary>
	/// Возвращает в ожидание источники, оставшиеся «в процессе» после падения или перезапуска воркера:
	/// суточный запуск пропускает такие источники, и без этого они не парсились бы больше никогда
	/// </summary>
	/// <returns></returns>
	public async Task RecoverAfterRestartAsync()
	{
		var count = await storage.ReleaseStuckInHandleAsync();
		if (count > 0)
		{
			logger.LogWarning("После перезапуска воркера возвращено в ожидание источников парсинга: {Count}", count);
		}
	}

	/// <summary>
	///     Раз в 4 дня
	/// </summary>
	[DisableConcurrentExecution(96 * 60 * 60)]
	public async Task ProcessMessagesAsync()
	{
		var ct = lifetime.ApplicationStopping;
		await TryWriteAsync(() => statusStorage.ReportStartedAsync(WorkerJobNames.ParseChannels, ct));

		try
		{
			await ParseSourcesAsync(ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			await TryWriteAsync(() => statusStorage.ReportFailedAsync(
				WorkerJobNames.ParseChannels,
				ex.Message,
				nextRun.GetNextRunAt(WorkerJobNames.ParseChannels),
				CancellationToken.None));
			throw;
		}

		// При остановке статус не пишем: запись останется Running, и API покажет «нет сигнала» по протухшему heartbeat'у
		if (!ct.IsCancellationRequested)
		{
			await TryWriteAsync(() => statusStorage.ReportCompletedAsync(
				WorkerJobNames.ParseChannels,
				nextRun.GetNextRunAt(WorkerJobNames.ParseChannels),
				CancellationToken.None));
		}
	}

	private async Task ParseSourcesAsync(CancellationToken ct)
	{
		logger.LogInformation("Начали парсинг каналов");
		var ids = await storage.GetChannelParsingParametersAsync();
		if (ids.Count == 0)
		{
			logger.LogInformation("Нет каналов которые нужно парсить");
			return;
		}

		await storage.SetInHandleStatusAsync(ids);

		for (var index = 0; index < ids.Count; index++)
		{
			var id = ids[index];
			var current = index;
			await TryWriteAsync(() => statusStorage.ReportHeartbeatAsync(
				WorkerJobNames.ParseChannels, current, ids.Count, null, ct));

			var startedAt = timeProvider.GetUtcNow();
			try
			{
				// Итоговый статус ставит сам парсинг: Waiting, Finished для разового источника или Failed
				await parseChannelUseCase.Handle(id, ct);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Во время парсинга произошла ошибка. Id настроек парсинга: {Id}.", id);
				await storage.SetErrorStatusAsync(id);

				// Остановка воркера прерывает парсинг отменой — это не поломка источника
				if (ct.IsCancellationRequested)
				{
					continue;
				}

				await TryWriteAsync(() => runStorage.AddRunAsync(
					ParseChannelRunReport.Failed(id, startedAt, timeProvider.GetUtcNow(), e.Message),
					CancellationToken.None));

				if (await storage.GetScheduleIdAsync(id, ct) is { } scheduleId)
				{
					await alertService.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed, e.Message,
						throttleKey: id, ct: ct);
				}
			}
		}
	}

	/// <summary>
	/// Записать статус задачи или журнал, проглатывая ошибки: сбой записи не должен останавливать парсинг
	/// остальных источников
	/// </summary>
	/// <param name="write"></param>
	private async Task TryWriteAsync(Func<Task> write)
	{
		try
		{
			await write();
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Не удалось записать статус задачи или журнал {JobName}", WorkerJobNames.ParseChannels);
		}
	}
}

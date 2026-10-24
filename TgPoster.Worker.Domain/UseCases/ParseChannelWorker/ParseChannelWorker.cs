using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

internal class ParseChannelWorker(
	IParseChannelWorkerStorage storage,
	ParseChannelUseCase parseChannelUseCase,
	IScheduleAlertService alertService,
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
		logger.LogInformation("Начали парсинг каналов");
		var ids = await storage.GetChannelParsingParametersAsync();
		if (ids.Count == 0)
		{
			logger.LogInformation("Нет каналов которые нужно парсить");
			return;
		}

		await storage.SetInHandleStatusAsync(ids);

		var ct = lifetime.ApplicationStopping;
		foreach (var id in ids)
		{
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
				if (!ct.IsCancellationRequested && await storage.GetScheduleIdAsync(id, ct) is { } scheduleId)
				{
					await alertService.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed, e.Message,
						throttleKey: id, ct: ct);
				}
			}
		}
	}
}
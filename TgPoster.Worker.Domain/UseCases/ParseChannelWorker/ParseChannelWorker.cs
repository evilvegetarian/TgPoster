using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

internal class ParseChannelWorker(
	IParseChannelWorkerStorage storage,
	ParseChannelUseCase parseChannelUseCase,
	ILogger<ParseChannelWorker> logger,
	IHostApplicationLifetime lifetime)
{
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

		foreach (var id in ids)
		{
			try
			{
				await parseChannelUseCase.Handle(id, lifetime.ApplicationStopping);
				await storage.SetWaitingStatusAsync(id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Во время парсинга произошла ошибка. Id настроек парсинга: {Id}.", id);
				await storage.SetErrorStatusAsync(id);
			}
		}
	}
}
using MassTransit;
using Microsoft.Extensions.Logging;
using TgPoster.Telegram.Models;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;

internal class ParseChannelConsumer(
	ILogger<ParseChannelConsumer> logger,
	IParseChannelConsumerStorage storage,
	ParseChannelUseCase parseChannelUseCase,
	IScheduleAlertService alertService)
	: IConsumer<ParseChannelContract>
{
	public async Task Consume(ConsumeContext<ParseChannelContract> context)
	{
		var id = context.Message.Id;
		var ct = context.CancellationToken;
		try
		{
			await storage.UpdateInHandleStatusAsync(id);
			logger.LogInformation("Получил запрос на парсинг канала: {Id}", id);
			await parseChannelUseCase.Handle(id, ct);
			logger.LogInformation("Спарсил канал: {Id}", id);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Ошибка во время попытки спарсить канал: {Id}", id);
			await storage.UpdateErrorStatusAsync(id);

			// Остановка воркера прерывает парсинг отменой — это не поломка источника
			if (!ct.IsCancellationRequested && await storage.GetScheduleIdAsync(id, ct) is { } scheduleId)
			{
				await alertService.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed, e.Message,
					throttleKey: id, ct: ct);
			}
		}
	}
}
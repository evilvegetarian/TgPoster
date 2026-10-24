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
	IParseChannelRunStorage runStorage,
	IScheduleAlertService alertService,
	TimeProvider timeProvider)
	: IConsumer<ParseChannelContract>
{
	public async Task Consume(ConsumeContext<ParseChannelContract> context)
	{
		var id = context.Message.Id;
		var ct = context.CancellationToken;
		var startedAt = timeProvider.GetUtcNow();
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
			if (ct.IsCancellationRequested)
			{
				return;
			}

			await AddFailedRunAsync(id, startedAt, e.Message);

			if (await storage.GetScheduleIdAsync(id, ct) is { } scheduleId)
			{
				await alertService.NotifyAsync(scheduleId, ScheduleProblem.ParsingFailed, e.Message,
					throttleKey: id, ct: ct);
			}
		}
	}

	/// <summary>
	/// Записать упавший проход в журнал; сбой записи только логируется, чтобы не запускать повтор сообщения
	/// </summary>
	/// <param name="id"></param>
	/// <param name="startedAt"></param>
	/// <param name="error"></param>
	private async Task AddFailedRunAsync(Guid id, DateTimeOffset startedAt, string error)
	{
		try
		{
			await runStorage.AddRunAsync(
				ParseChannelRunReport.Failed(id, startedAt, timeProvider.GetUtcNow(), error),
				CancellationToken.None);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Не удалось записать проход источника {Id} в журнал парсинга", id);
		}
	}
}
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;

internal class ParseChannelConsumer(
	ILogger<ParseChannelConsumer> logger,
	IParseChannelConsumerStorage storage,
	ParseChannelUseCase parseChannelUseCase)
	: IConsumer<ParseChannelContract>
{
	public async Task Consume(ConsumeContext<ParseChannelContract> context)
	{
		var id = context.Message.Id;
		try
		{
			await storage.UpdateInHandleStatusAsync(id);
			logger.LogInformation("Получил запрос на парсинг канала: {Id}", id);
			await parseChannelUseCase.Handle(id, CancellationToken.None);
			logger.LogInformation("Спарсил канал: {Id}", id);
		}
		catch (Exception e)
		{
			logger.LogError(e, "Ошибка во время попытки спарсить канал: {Id}", id);
			await storage.UpdateErrorStatusAsync(id);
		}
	}
}
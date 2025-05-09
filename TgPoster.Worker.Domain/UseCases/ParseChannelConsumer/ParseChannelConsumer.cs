using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;

internal class ParseChannelConsumer(
    ILogger<ParseChannelConsumer> logger,
    ParseChannelUseCase parseChannelUseCase)
    : IConsumer<ParseChannelContract>
{
    public async Task Consume(ConsumeContext<ParseChannelContract> context)
    {
        try
        {
            logger.LogInformation("Получил запрос на парсинг канала: {Id}", context.Message.Id);
            await parseChannelUseCase.Handle(context.Message.Id, CancellationToken.None);
            logger.LogInformation("Спарсил канал: {Id}", context.Message.Id);
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка во время попытки спарсить канал: {Id} Ошибка: {ex}", context.Message.Id, e.Message);
        }
    }
}
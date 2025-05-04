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
            logger.LogInformation($"Parse channel consumed: {context.Message.Id}");
            await parseChannelUseCase.Handle(context.Message.Id, CancellationToken.None);
            logger.LogInformation($"Parse channel consumed2 {context.Message.Id}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
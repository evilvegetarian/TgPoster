using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;

public class ParseChannelConsumer(ILogger<ParseChannelWorker.ParseChannelWorker> logger, ParseChannelUseCase parseChannelUseCase)
    : IConsumer<ParseChannelContract>
{
    public async Task Consume(ConsumeContext<ParseChannelContract> context)
    {
        logger.LogInformation($"Parse channel consumed: {context.Message.Id}");
        await parseChannelUseCase.Handle(context.Message.Id);
        logger.LogInformation($"Parse channel consumed2 {context.Message.Id}");
    }
}
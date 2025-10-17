using Microsoft.Extensions.Logging;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

internal class ParseChannelWorker(
    IParseChannelWorkerStorage storage,
    ParseChannelUseCase parseChannelUseCase,
    ILogger<ParseChannelWorker> logger)
{
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
                await parseChannelUseCase.Handle(id, CancellationToken.None);
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
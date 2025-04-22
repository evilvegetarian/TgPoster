using Microsoft.Extensions.Logging;
using TgPoster.Worker.Domain.UseCases.ParseChannel;

namespace TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

public class ParseChannelWorker(
    IParseChannelWorkerStorage storage,
    ParseChannelUseCase parseChannelUseCase,
    Logger<ParseChannelWorker> logger)
{
    public async Task ProcessMessagesAsync()
    {
        var parametrs = await storage.GetParameters();
        foreach (var pr in parametrs)
        {
            await parseChannelUseCase.Handle(pr, CancellationToken.None);
            logger.LogInformation($"Partis: {pr}");
        }
    }
}

public interface IParseChannelWorkerStorage
{
    //TODO: Нужно статус обновлять
    public Task<List<Guid>> GetParameters();
}
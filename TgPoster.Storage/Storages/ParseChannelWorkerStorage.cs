using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ParseChannelWorker;

namespace TgPoster.Storage.Storages;

public class ParseChannelWorkerStorage(PosterContext context) : IParseChannelWorkerStorage
{
    public Task<List<Guid>> GetChannelParsingParametersAsync()
    {
        return context.ChannelParsingParameters
            .Where(x =>
                x.Status == ParsingStatus.InHandle
                || x.Status == ParsingStatus.Finished)
            .Where(x => x.CheckNewPosts)
            .Select(x => x.Id)
            .ToListAsync();
    }

    public Task SetInHandleStatusAsync(List<Guid> ids)
    {
        return context.ChannelParsingParameters
            .Where(x => ids.Contains(x.Id))
            .ExecuteUpdateAsync(updater =>
                updater.SetProperty(
                    x => x.Status, ParsingStatus.InHandle));
    }


    public async Task SetWaitingStatusAsync(Guid id)
    {
        var channelParsingParameters = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == id);
        channelParsingParameters!.Status = ParsingStatus.Waiting;
        await context.SaveChangesAsync();
    }
}
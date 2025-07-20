using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.ParseChannelConsumer;

namespace TgPoster.Storage.Storages;

internal class ParseChannelConsumerStorage(PosterContext context) : IParseChannelConsumerStorage
{
    public async Task UpdateInHandleStatusAsync(Guid id)
    {
        var channelParsingParameters = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == id);
        channelParsingParameters!.Status = ParsingStatus.InHandle;
        await context.SaveChangesAsync();
    }

    public async Task UpdateErrorStatusAsync(Guid id)
    {
        var channelParsingParameters = await context.ChannelParsingParameters.FirstOrDefaultAsync(x => x.Id == id);
        channelParsingParameters!.Status = ParsingStatus.Failed;
        await context.SaveChangesAsync();
    }
}
using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannel;
using TgPoster.Storage.Data;
using TgPoster.Storage.Mapper;

namespace TgPoster.Storage.Storages;

internal class ListParseChannelsStorage(PosterContext context) : IListParseChannelsStorage
{
    public async Task<List<ParseChannelsResponse>> GetChannelAsync(Guid userId, CancellationToken ct)
    {
        var parametersList = await context.ChannelParsingParameters
            .Where(x => x.Schedule.UserId == userId)
            .ToListAsync(ct);

        return parametersList.Select(x => x.ToDomain()).ToList();
    }
}
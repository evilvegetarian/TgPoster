using TgPoster.API.Domain.Models;

namespace TgPoster.API.Domain.UseCases.Discover.ListDiscover;

public interface IListDiscoverStorage
{
    Task<PagedList<DiscoverChannelResponse>> GetDiscoverChannelsAsync(ListDiscoverQuery query, CancellationToken ct);
}

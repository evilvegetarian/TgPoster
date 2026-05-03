using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

internal static class ListDiscoverRequestMapper
{
    public static ListDiscoverQuery ToDomain(this ListDiscoverRequest request) =>
        new(request.PageNumber, request.PageSize, request.Category, request.Search, request.PeerType);
}

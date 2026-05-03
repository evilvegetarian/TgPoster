using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Discover.GetCategories;
using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class DiscoverStorage(PosterContext context) : IListDiscoverStorage, IGetCategoriesStorage
{
    public async Task<PagedList<DiscoverChannelResponse>> GetDiscoverChannelsAsync(
        ListDiscoverQuery query, CancellationToken ct)
    {
        var q = context.DiscoveredChannels
            .Where(x => x.Status == DiscoveryStatus.Completed)
            .Where(x => query.Category == null || x.Category == query.Category)
            .Where(x => query.PeerType == null || x.PeerType == query.PeerType)
            .Where(x => query.Search == null
                || (x.Title != null && x.Title.Contains(query.Search))
                || (x.Username != null && x.Username.Contains(query.Search)));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.ParticipantsCount)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new DiscoverChannelResponse
            {
                Id = x.Id,
                Username = x.Username,
                Title = x.Title,
                Description = x.Description,
                AvatarUrl = x.AvatarUrl,
                ParticipantsCount = x.ParticipantsCount,
                PeerType = x.PeerType,
                TgUrl = x.TgUrl,
                Category = x.Category,
                Subcategory = x.Subcategory,
                Language = x.Language,
                LastDiscoveredAt = x.LastDiscoveredAt,
            })
            .ToListAsync(ct);

        return new PagedList<DiscoverChannelResponse>(items, total);
    }

    public Task<List<string>> GetCategoriesAsync(CancellationToken ct) =>
        context.DiscoveredChannels
            .Where(x => x.Category != null && x.Status == DiscoveryStatus.Completed)
            .Select(x => x.Category!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
}

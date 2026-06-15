using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Discover.GetCategories;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;
using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain;

namespace TgPoster.Storage.Storages;

internal sealed class DiscoverStorage(PosterContext context)
    : IListDiscoverStorage, IGetCategoriesStorage, IGetDiscoverStatusStorage
{
    public async Task<PagedList<DiscoverChannelResponse>> GetDiscoverChannelsAsync(
        ListDiscoverQuery query, CancellationToken ct)
    {
        var q = context.DiscoveredChannels
            .Where(x => query.Category == null || x.Category == query.Category)
            .Where(x => query.PeerType == null || x.PeerType == query.PeerType)
            .Where(x => query.MinParticipants == null
                || (x.ParticipantsCount != null && x.ParticipantsCount >= query.MinParticipants))
            .Where(x => query.MaxParticipants == null
                || (x.ParticipantsCount != null && x.ParticipantsCount <= query.MaxParticipants))
            .Where(x => query.Search == null
                || (x.Title != null && x.Title.Contains(query.Search))
                || (x.Username != null && x.Username.Contains(query.Search)));

        var total = await q.CountAsync(ct);

        q = query.SortBy switch
        {
            DiscoverSortBy.DiscoveredAt => query.SortDirection == SortDirection.Asc
                ? q.OrderBy(x => x.LastDiscoveredAt)
                : q.OrderByDescending(x => x.LastDiscoveredAt),
            DiscoverSortBy.Title => query.SortDirection == SortDirection.Asc
                ? q.OrderBy(x => x.Title)
                : q.OrderByDescending(x => x.Title),
            _ => query.SortDirection == SortDirection.Asc
                ? q.OrderBy(x => x.ParticipantsCount)
                : q.OrderByDescending(x => x.ParticipantsCount)
        };

        var items = await q
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

    public Task<WorkerJobStateDto?> GetDiscoverJobStateAsync(CancellationToken ct) =>
        context.WorkerJobStates
            .Where(x => x.JobName == WorkerJobNames.DiscoverChannelLinks)
            .Select(x => new WorkerJobStateDto(
                (WorkerJobStateStatus)x.Status,
                x.LastStartedAt,
                x.LastFinishedAt,
                x.HeartbeatAt,
                x.CooldownUntil,
                x.NextRunAt,
                x.LastError,
                x.ProgressCurrent,
                x.ProgressTotal,
                x.ProgressMessage))
            .FirstOrDefaultAsync(ct);
}

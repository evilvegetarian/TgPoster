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

	public async Task<PagedList<DiscoverChannelResponse>> GetDiscoverChannelsAsync(
		ListDiscoverQuery query,
		CancellationToken ct
	)
	{
		var q = context.DiscoveredChannels
			.ApplyDiscoverFilter(
				query.Category,
				query.PeerType,
				query.Search,
				query.MinParticipants,
				query.MaxParticipants);

		var total = await q.CountAsync(ct);

		var items = await q
			.ApplyDiscoverSort(query.SortBy, query.SortDirection)
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
				LastDiscoveredAt = x.LastDiscoveredAt
			})
			.ToListAsync(ct);

		return new PagedList<DiscoverChannelResponse>(items, total);
	}
}
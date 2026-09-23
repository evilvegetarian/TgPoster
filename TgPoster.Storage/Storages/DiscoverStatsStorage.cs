using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Discover;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class DiscoverStatsStorage(PosterContext context)
	: IGetDiscoverStatsStorage, IGetDiscoverParseHistoryStorage
{
	private const int Participants1K = 1_000;
	private const int Participants10K = 10_000;
	private const int Participants100K = 100_000;

	public async Task<DiscoverStatsTotalsDto> GetTotalsAsync(DateTimeOffset now, CancellationToken ct)
	{
		var dayAgo = now.AddHours(-24);
		var weekAgo = now.AddDays(-7);
		var monthAgo = now.AddDays(-30);

		// Все счётчики считаем одним проходом по таблице: GroupBy по константе
		// превращается в набор агрегатов с FILTER. Строка всегда максимум одна,
		// но на пустой таблице группы нет вовсе — отсюда SingleOrDefault и фолбэк
		var totals = await context.DiscoveredChannels
			.GroupBy(_ => 1)
			.Select(g => new DiscoverStatsTotalsDto(
				g.Count(),
				g.Count(x => x.LastDiscoveredAt != null),
				g.Count(x => x.Username != null),
				g.Count(x => x.Category != null),
				g.Count(x => x.ParticipantsCount != null),
				g.Sum(x => (long?)x.ParticipantsCount) ?? 0,
				g.Count(x => x.LastDiscoveredAt >= dayAgo),
				g.Count(x => x.LastDiscoveredAt >= weekAgo),
				g.Count(x => x.LastDiscoveredAt >= monthAgo),
				g.Count(x => x.Created >= dayAgo),
				g.Count(x => x.Created >= weekAgo),
				g.Count(x => x.Created >= monthAgo),
				g.Min(x => x.Created),
				g.Max(x => x.Created),
				g.Max(x => x.LastDiscoveredAt)))
			.SingleOrDefaultAsync(ct);

		return totals ?? DiscoverStatsTotalsDto.Empty;
	}

	public Task<int> GetBannedCountAsync(CancellationToken ct) =>
		context.DiscoveredChannels
			.IgnoreQueryFilters()
			.CountAsync(x => x.IsBanned, ct);

	public Task<List<DiscoverStatusCount>> GetStatusCountsAsync(CancellationToken ct) =>
		context.DiscoveredChannels
			.GroupBy(x => x.Status)
			.Select(g => new DiscoverStatusCount
			{
				Status = (DiscoverChannelStatus)g.Key,
				Count = g.Count()
			})
			.ToListAsync(ct);

	public Task<List<DiscoverNamedCount>> GetPeerTypeCountsAsync(CancellationToken ct) =>
		context.DiscoveredChannels
			.GroupBy(x => x.PeerType ?? string.Empty)
			.Select(g => new DiscoverNamedCount
			{
				Name = g.Key,
				Count = g.Count()
			})
			.ToListAsync(ct);

	public Task<List<DiscoverNamedCount>> GetCategoryCountsAsync(CancellationToken ct) =>
		context.DiscoveredChannels
			.Where(x => x.Category != null)
			.GroupBy(x => x.Category!)
			.Select(g => new DiscoverNamedCount
			{
				Name = g.Key,
				Count = g.Count()
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Name)
			.ToListAsync(ct);

	public Task<List<DiscoverNamedCount>> GetLanguageCountsAsync(CancellationToken ct) =>
		context.DiscoveredChannels
			.Where(x => x.Language != null)
			.GroupBy(x => x.Language!)
			.Select(g => new DiscoverNamedCount
			{
				Name = g.Key,
				Count = g.Count()
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Name)
			.ToListAsync(ct);

	public async Task<List<DiscoverParticipantsBucketCount>> GetParticipantsBucketCountsAsync(CancellationToken ct)
	{
		var counts = await context.DiscoveredChannels
			.GroupBy(_ => 1)
			.Select(g => new
			{
				Unknown = g.Count(x => x.ParticipantsCount == null),
				UpTo1K = g.Count(x => x.ParticipantsCount < Participants1K),
				From1KTo10K = g.Count(x => x.ParticipantsCount >= Participants1K
				                           && x.ParticipantsCount < Participants10K),
				From10KTo100K = g.Count(x => x.ParticipantsCount >= Participants10K
				                             && x.ParticipantsCount < Participants100K),
				Over100K = g.Count(x => x.ParticipantsCount >= Participants100K)
			})
			.SingleOrDefaultAsync(ct);

		if (counts is null)
		{
			return [];
		}

		return
		[
			new DiscoverParticipantsBucketCount { Bucket = DiscoverParticipantsBucket.Unknown, Count = counts.Unknown },
			new DiscoverParticipantsBucketCount { Bucket = DiscoverParticipantsBucket.UpTo1K, Count = counts.UpTo1K },
			new DiscoverParticipantsBucketCount
				{ Bucket = DiscoverParticipantsBucket.From1KTo10K, Count = counts.From1KTo10K },
			new DiscoverParticipantsBucketCount
				{ Bucket = DiscoverParticipantsBucket.From10KTo100K, Count = counts.From10KTo100K },
			new DiscoverParticipantsBucketCount { Bucket = DiscoverParticipantsBucket.Over100K, Count = counts.Over100K }
		];
	}

	public Task<List<DiscoverDailyCount>> GetParsedByDayAsync(DateTimeOffset since, CancellationToken ct) =>
		context.DiscoveredChannels
			.Where(x => x.LastDiscoveredAt != null && x.LastDiscoveredAt >= since)
			.GroupBy(x => DateOnly.FromDateTime(x.LastDiscoveredAt!.Value.UtcDateTime))
			.Select(g => new DiscoverDailyCount { Date = g.Key, Count = g.Count() })
			.OrderBy(x => x.Date)
			.ToListAsync(ct);

	public Task<List<DiscoverDailyCount>> GetFoundByDayAsync(DateTimeOffset since, CancellationToken ct) =>
		context.DiscoveredChannels
			.Where(x => x.Created != null && x.Created >= since)
			.GroupBy(x => DateOnly.FromDateTime(x.Created!.Value.UtcDateTime))
			.Select(g => new DiscoverDailyCount { Date = g.Key, Count = g.Count() })
			.OrderBy(x => x.Date)
			.ToListAsync(ct);

	public async Task<List<DiscoverSourceStat>> GetTopSourcesAsync(int limit, CancellationToken ct)
	{
		var top = await context.DiscoveredChannels
			.Where(x => x.DiscoveredFromChannelId != null)
			.GroupBy(x => x.DiscoveredFromChannelId!.Value)
			.Select(g => new { SourceId = g.Key, Count = g.Count() })
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.SourceId)
			.Take(limit)
			.ToListAsync(ct);

		if (top.Count == 0)
		{
			return [];
		}

		var sourceIds = top.Select(x => x.SourceId).ToArray();

		// IgnoreQueryFilters: источник мог быть забанен уже после того, как из него
		// набрали каналы, и в топе он всё равно должен быть виден
		var sources = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.Where(x => sourceIds.Contains(x.Id))
			.Select(x => new
			{
				x.Id,
				x.Username,
				x.Title,
				x.AvatarUrl,
				x.TgUrl,
				x.LastDiscoveredAt,
				x.IsBanned
			})
			.ToDictionaryAsync(x => x.Id, ct);

		return top
			.Select(x =>
			{
				var source = sources.GetValueOrDefault(x.SourceId);
				return new DiscoverSourceStat
				{
					Id = x.SourceId,
					Username = source?.Username,
					Title = source?.Title,
					AvatarUrl = source?.AvatarUrl,
					TgUrl = source?.TgUrl,
					FoundCount = x.Count,
					LastParsedAt = source?.LastDiscoveredAt,
					IsBanned = source?.IsBanned ?? false
				};
			})
			.ToList();
	}

	public async Task<PagedList<DiscoverParseHistoryItemResponse>> GetParseHistoryAsync(
		GetDiscoverParseHistoryQuery query,
		CancellationToken ct
	)
	{
		var q = context.DiscoveredChannels
			.Where(x => x.LastDiscoveredAt != null)
			.Where(x => query.From == null || x.LastDiscoveredAt >= query.From)
			.Where(x => query.To == null || x.LastDiscoveredAt <= query.To)
			.Where(x => query.Search == null
			            || (x.Title != null && x.Title.Contains(query.Search))
			            || (x.Username != null && x.Username.Contains(query.Search)));

		var total = await q.CountAsync(ct);

		var items = await q
			.OrderByDescending(x => x.LastDiscoveredAt)
			.ThenBy(x => x.Id)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.Select(x => new DiscoverParseHistoryItemResponse
			{
				Id = x.Id,
				Username = x.Username,
				Title = x.Title,
				AvatarUrl = x.AvatarUrl,
				TgUrl = x.TgUrl,
				PeerType = x.PeerType,
				Category = x.Category,
				ParticipantsCount = x.ParticipantsCount,
				Status = (DiscoverChannelStatus)x.Status,
				ParsedAt = x.LastDiscoveredAt!.Value,
				FoundAt = x.Created,
				LastParsedMessageId = x.LastParsedId,
				FoundCount = context.DiscoveredChannels.Count(c => c.DiscoveredFromChannelId == x.Id),
				SourceTitle = x.DiscoveredFromChannel != null ? x.DiscoveredFromChannel.Title : null,
				SourceUsername = x.DiscoveredFromChannel != null ? x.DiscoveredFromChannel.Username : null
			})
			.ToListAsync(ct);

		return new PagedList<DiscoverParseHistoryItemResponse>(items, total);
	}
}

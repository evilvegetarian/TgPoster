using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStats;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStatus;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStats;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Worker.Domain;

namespace TgPoster.Storage.Storages;

internal sealed class ClassificationStatsStorage(PosterContext context)
	: IGetClassificationStatsStorage, IGetClassificationHistoryStorage, IGetClassificationStatusStorage
{
	private const double Confidence50 = 0.5;
	private const double Confidence70 = 0.7;
	private const double Confidence80 = 0.8;
	private const double Confidence90 = 0.9;

	public async Task<ClassificationStatsTotalsDto> GetTotalsAsync(DateTimeOffset now, CancellationToken ct)
	{
		var dayAgo = now.AddHours(-24);
		var weekAgo = now.AddDays(-7);
		var monthAgo = now.AddDays(-30);

		// Как и в статистике Discover — один проход по таблице агрегатами с FILTER;
		// на пустой таблице группы нет вовсе, отсюда SingleOrDefault и фолбэк
		var totals = await context.DiscoveredChannels
			.GroupBy(_ => 1)
			.Select(g => new ClassificationStatsTotalsDto(
				g.Count(),
				g.Count(x => x.Username != null),
				g.Count(x => x.LastClassifiedAt != null),
				g.Count(x => x.Username != null && x.LastClassifiedAt == null),
				g.Count(x => x.Category != null),
				g.Count(x => x.Tags != null && x.Tags.Length > 0),
				g.Average(x => x.ClassificationConfidence),
				g.Count(x => x.LastClassifiedAt >= dayAgo),
				g.Count(x => x.LastClassifiedAt >= weekAgo),
				g.Count(x => x.LastClassifiedAt >= monthAgo),
				g.Max(x => x.LastClassifiedAt)))
			.SingleOrDefaultAsync(ct);

		return totals ?? ClassificationStatsTotalsDto.Empty;
	}

	public Task<List<ClassificationCategoryStat>> GetCategoryStatsAsync(CancellationToken ct) =>
		context.DiscoveredChannels
			.Where(x => x.Category != null)
			.GroupBy(x => x.Category!)
			.Select(g => new ClassificationCategoryStat
			{
				Name = g.Key,
				Count = g.Count(),
				AverageConfidence = g.Average(x => x.ClassificationConfidence)
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

	public async Task<List<ClassificationConfidenceBucketCount>> GetConfidenceBucketCountsAsync(CancellationToken ct)
	{
		var counts = await context.DiscoveredChannels
			.Where(x => x.LastClassifiedAt != null)
			.GroupBy(_ => 1)
			.Select(g => new
			{
				Unknown = g.Count(x => x.ClassificationConfidence == null),
				UpTo50 = g.Count(x => x.ClassificationConfidence < Confidence50),
				From50To70 = g.Count(x => x.ClassificationConfidence >= Confidence50
				                          && x.ClassificationConfidence < Confidence70),
				From70To80 = g.Count(x => x.ClassificationConfidence >= Confidence70
				                          && x.ClassificationConfidence < Confidence80),
				From80To90 = g.Count(x => x.ClassificationConfidence >= Confidence80
				                          && x.ClassificationConfidence < Confidence90),
				Over90 = g.Count(x => x.ClassificationConfidence >= Confidence90)
			})
			.SingleOrDefaultAsync(ct);

		if (counts is null)
		{
			return [];
		}

		return
		[
			new ClassificationConfidenceBucketCount
				{ Bucket = ClassificationConfidenceBucket.Unknown, Count = counts.Unknown },
			new ClassificationConfidenceBucketCount
				{ Bucket = ClassificationConfidenceBucket.UpTo50, Count = counts.UpTo50 },
			new ClassificationConfidenceBucketCount
				{ Bucket = ClassificationConfidenceBucket.From50To70, Count = counts.From50To70 },
			new ClassificationConfidenceBucketCount
				{ Bucket = ClassificationConfidenceBucket.From70To80, Count = counts.From70To80 },
			new ClassificationConfidenceBucketCount
				{ Bucket = ClassificationConfidenceBucket.From80To90, Count = counts.From80To90 },
			new ClassificationConfidenceBucketCount
				{ Bucket = ClassificationConfidenceBucket.Over90, Count = counts.Over90 }
		];
	}

	public Task<List<ClassificationSubcategoryStat>> GetTopSubcategoriesAsync(int limit, CancellationToken ct) =>
		context.DiscoveredChannels
			.Where(x => x.Subcategory != null)
			.GroupBy(x => new { x.Category, Subcategory = x.Subcategory! })
			.Select(g => new ClassificationSubcategoryStat
			{
				Category = g.Key.Category,
				Subcategory = g.Key.Subcategory,
				Count = g.Count()
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Subcategory)
			.Take(limit)
			.ToListAsync(ct);

	public Task<int> GetDistinctSubcategoryCountAsync(CancellationToken ct) =>
		context.DiscoveredChannels
			.Where(x => x.Subcategory != null)
			.Select(x => new { x.Category, x.Subcategory })
			.Distinct()
			.CountAsync(ct);

	public Task<List<DiscoverNamedCount>> GetTopTagsAsync(int limit, CancellationToken ct) =>
		NormalizedTags()
			.GroupBy(tag => tag)
			.Select(g => new DiscoverNamedCount
			{
				Name = g.Key,
				Count = g.Count()
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Name)
			.Take(limit)
			.ToListAsync(ct);

	public Task<int> GetDistinctTagCountAsync(CancellationToken ct) =>
		NormalizedTags()
			.Distinct()
			.CountAsync(ct);

	public Task<List<DiscoverDailyCount>> GetClassifiedByDayAsync(DateTimeOffset since, CancellationToken ct) =>
		context.DiscoveredChannels
			.Where(x => x.LastClassifiedAt != null && x.LastClassifiedAt >= since)
			.GroupBy(x => DateOnly.FromDateTime(x.LastClassifiedAt!.Value.UtcDateTime))
			.Select(g => new DiscoverDailyCount { Date = g.Key, Count = g.Count() })
			.OrderBy(x => x.Date)
			.ToListAsync(ct);

	public async Task<PagedList<ClassificationHistoryItemResponse>> GetClassificationHistoryAsync(
		GetClassificationHistoryQuery query,
		CancellationToken ct
	)
	{
		var q = context.DiscoveredChannels
			.Where(x => x.LastClassifiedAt != null)
			.Where(x => query.From == null || x.LastClassifiedAt >= query.From)
			.Where(x => query.To == null || x.LastClassifiedAt <= query.To)
			.Where(x => query.Category == null || x.Category == query.Category)
			.Where(x => query.Search == null
			            || (x.Title != null && x.Title.Contains(query.Search))
			            || (x.Username != null && x.Username.Contains(query.Search)));
		q = FilterByConfidence(q, query.Confidence);

		var total = await q.CountAsync(ct);

		var items = await q
			.OrderByDescending(x => x.LastClassifiedAt)
			.ThenBy(x => x.Id)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.Select(x => new ClassificationHistoryItemResponse
			{
				Id = x.Id,
				Username = x.Username,
				Title = x.Title,
				AvatarUrl = x.AvatarUrl,
				TgUrl = x.TgUrl,
				ParticipantsCount = x.ParticipantsCount,
				Category = x.Category,
				Subcategory = x.Subcategory,
				Tags = x.Tags ?? Array.Empty<string>(),
				Language = x.Language,
				Confidence = x.ClassificationConfidence,
				ClassifiedAt = x.LastClassifiedAt!.Value
			})
			.ToListAsync(ct);

		return new PagedList<ClassificationHistoryItemResponse>(items, total);
	}

	public Task<WorkerJobStateDto?> GetClassificationJobStateAsync(CancellationToken ct) =>
		context.WorkerJobStates
			.Where(x => x.JobName == WorkerJobNames.ClassifyChannels)
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

	/// <summary>
	///     Все теги всех каналов, приведённые к нижнему регистру и без пустых: модель пишет
	///     один и тот же тег то с большой, то с маленькой буквы
	/// </summary>
	/// <returns></returns>
	private IQueryable<string> NormalizedTags() =>
		context.DiscoveredChannels
			.Where(x => x.Tags != null)
			.SelectMany(x => x.Tags!)
			.Select(tag => tag.Trim().ToLower())
			.Where(tag => tag != "");

	/// <summary>
	///     Оставить только каналы, чья уверенность попадает в указанную корзину
	/// </summary>
	/// <param name="query"></param>
	/// <param name="bucket"></param>
	/// <returns></returns>
	private static IQueryable<DiscoveredChannel> FilterByConfidence(
		IQueryable<DiscoveredChannel> query,
		ClassificationConfidenceBucket? bucket
	) =>
		bucket switch
		{
			null => query,
			ClassificationConfidenceBucket.Unknown => query.Where(x => x.ClassificationConfidence == null),
			ClassificationConfidenceBucket.UpTo50 => query.Where(x => x.ClassificationConfidence < Confidence50),
			ClassificationConfidenceBucket.From50To70 => query.Where(x => x.ClassificationConfidence >= Confidence50
			                                                              && x.ClassificationConfidence < Confidence70),
			ClassificationConfidenceBucket.From70To80 => query.Where(x => x.ClassificationConfidence >= Confidence70
			                                                              && x.ClassificationConfidence < Confidence80),
			ClassificationConfidenceBucket.From80To90 => query.Where(x => x.ClassificationConfidence >= Confidence80
			                                                              && x.ClassificationConfidence < Confidence90),
			ClassificationConfidenceBucket.Over90 => query.Where(x => x.ClassificationConfidence >= Confidence90),
			_ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null)
		};
}

using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;
using TgPoster.API.Domain.UseCases.Parse.GetParseChannelStats;
using TgPoster.API.Domain.UseCases.Parse.GetParseChannelStatus;
using TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain;

namespace TgPoster.Storage.Storages;

internal sealed class ParseChannelStatsStorage(PosterContext context)
	: IGetParseChannelStatsStorage, IListParseChannelRunsStorage, IGetParseChannelStatusStorage
{
	public async Task<List<ParseChannelSourceStatusCount>> GetSourceStatusCountsAsync(
		Guid userId,
		CancellationToken ct
	)
	{
		var counts = await context.ChannelParsingParameters
			.Where(x => x.Schedule.UserId == userId)
			.GroupBy(x => x.Status)
			.Select(g => new { Status = g.Key, Count = g.Count() })
			.ToListAsync(ct);

		return counts.ConvertAll(x => new ParseChannelSourceStatusCount
		{
			Status = (ParseChannelSourceStatus)(int)x.Status,
			Count = x.Count
		});
	}

	public async Task<ParseChannelPostTotals> GetPostTotalsAsync(Guid userId, CancellationToken ct)
	{
		var counts = await context.Messages
			.Where(m => m.ChannelParsingSettingId != null && m.Schedule.UserId == userId)
			.GroupBy(m => new { m.Status, m.IsVerified })
			.Select(g => new { g.Key.Status, g.Key.IsVerified, Count = g.Count() })
			.ToListAsync(ct);

		return new ParseChannelPostTotals
		{
			Total = counts.Sum(x => x.Count),
			Planned = counts
				.Where(x => x.Status == MessageStatus.InHandle || x.Status == MessageStatus.Sending || (x.Status == MessageStatus.Register && x.IsVerified))
				.Sum(x => x.Count),
			AwaitingApproval = counts.Where(x => x.Status == MessageStatus.Register && !x.IsVerified).Sum(x => x.Count),
			Published = counts.Where(x => x.Status == MessageStatus.Send).Sum(x => x.Count),
			Failed = counts.Where(x => x.Status == MessageStatus.Error).Sum(x => x.Count),
			Canceled = counts.Where(x => x.Status == MessageStatus.Cancel).Sum(x => x.Count)
		};
	}

	public async Task<ParseChannelRunTotals> GetRunTotalsAsync(
		Guid userId,
		DateTimeOffset since,
		CancellationToken ct
	)
	{
		// GroupBy по константе превращается в один набор агрегатов; на пустом окне группы нет вовсе
		var totals = await UserRuns(userId)
			.Where(r => r.StartedAt >= since)
			.GroupBy(_ => 1)
			.Select(g => new ParseChannelRunTotals
			{
				Runs = g.Count(),
				FailedRuns = g.Count(r => r.Status != ParseChannelRunStatus.Success),
				FetchedMessages = g.Sum(r => r.FetchedMessages),
				Posts = g.Sum(r => r.Posts),
				SkippedByAvoidWords = g.Sum(r => r.SkippedByAvoidWords),
				SkippedAsAds = g.Sum(r => r.SkippedAsAds),
				SkippedAsDuplicates = g.Sum(r => r.SkippedAsDuplicates),
				SkippedEmpty = g.Sum(r => r.SkippedEmpty),
				Published = g.Sum(r => r.Published)
			})
			.SingleOrDefaultAsync(ct);

		return totals ?? ParseChannelRunTotals.Empty;
	}

	public Task<List<ParseChannelRunDailyDto>> GetRunsByDayAsync(
		Guid userId,
		DateTimeOffset since,
		CancellationToken ct
	) =>
		UserRuns(userId)
			.Where(r => r.StartedAt >= since)
			.GroupBy(r => DateOnly.FromDateTime(r.StartedAt.UtcDateTime))
			.Select(g => new ParseChannelRunDailyDto(
				g.Key,
				g.Sum(r => r.Published),
				g.Sum(r => r.SkippedByAvoidWords + r.SkippedAsAds + r.SkippedAsDuplicates + r.SkippedEmpty)))
			.ToListAsync(ct);

	public async Task<List<ParseChannelSourceStat>> GetTopSourcesAsync(
		Guid userId,
		DateTimeOffset since,
		int limit,
		CancellationToken ct
	)
	{
		var sources = await context.ChannelParsingParameters
			.Where(x => x.Schedule.UserId == userId)
			.Select(x => new
			{
				x.Id,
				x.Channel,
				ScheduleName = x.Schedule.Name,
				x.Status,
				Runs = x.Runs.Count(r => r.StartedAt >= since),
				FailedRuns = x.Runs.Count(r => r.StartedAt >= since && r.Status != ParseChannelRunStatus.Success),
				Published = x.Runs.Where(r => r.StartedAt >= since).Sum(r => r.Published),
				Skipped = x.Runs
					.Where(r => r.StartedAt >= since)
					.Sum(r => r.SkippedByAvoidWords + r.SkippedAsAds + r.SkippedAsDuplicates + r.SkippedEmpty),
				LastRunAt = x.Runs.Max(r => (DateTimeOffset?)r.FinishedAt),
				LastRunStatus = x.Runs
					.OrderByDescending(r => r.StartedAt)
					.Select(r => (ParseChannelRunStatus?)r.Status)
					.FirstOrDefault(),
				ParsedMessagesCount = x.ParsedMessages.Count(),
				x.TotalMessagesCount
			})
			.OrderByDescending(x => x.Published)
			.ThenBy(x => x.Channel)
			.Take(limit)
			.ToListAsync(ct);

		return sources.ConvertAll(x => new ParseChannelSourceStat
		{
			Id = x.Id,
			Channel = x.Channel,
			ScheduleName = x.ScheduleName,
			Status = (ParseChannelSourceStatus)(int)x.Status,
			Runs = x.Runs,
			FailedRuns = x.FailedRuns,
			Published = x.Published,
			Skipped = x.Skipped,
			LastRunAt = x.LastRunAt,
			LastRunStatus = x.LastRunStatus,
			ParsedMessagesCount = x.ParsedMessagesCount,
			TotalMessagesCount = x.TotalMessagesCount
		});
	}

	public Task<DateTimeOffset?> GetLastRunAtAsync(Guid userId, CancellationToken ct) =>
		UserRuns(userId).MaxAsync(r => (DateTimeOffset?)r.FinishedAt, ct);

	public async Task<PagedList<ParseChannelRunResponse>> GetRunsAsync(
		ListParseChannelRunsQuery query,
		Guid userId,
		CancellationToken ct
	)
	{
		var runs = UserRuns(userId);
		if (query.ParseChannelId is { } parseChannelId)
		{
			runs = runs.Where(r => r.ChannelParsingSettingId == parseChannelId);
		}

		if (query.Status is { } status)
		{
			runs = runs.Where(r => r.Status == status);
		}

		if (query.From is { } from)
		{
			runs = runs.Where(r => r.StartedAt >= from);
		}

		if (query.To is { } to)
		{
			runs = runs.Where(r => r.StartedAt <= to);
		}

		var total = await runs.CountAsync(ct);

		var items = await runs
			.OrderByDescending(r => r.StartedAt)
			.ThenBy(r => r.Id)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.Select(r => new ParseChannelRunResponse
			{
				Id = r.Id,
				ParseChannelId = r.ChannelParsingSettingId,
				Channel = r.ChannelParsingSetting.Channel,
				ScheduleName = r.ChannelParsingSetting.Schedule.Name,
				StartedAt = r.StartedAt,
				FinishedAt = r.FinishedAt,
				Status = r.Status,
				Error = r.Error,
				FetchedMessages = r.FetchedMessages,
				Posts = r.Posts,
				SkippedByAvoidWords = r.SkippedByAvoidWords,
				SkippedAsAds = r.SkippedAsAds,
				SkippedAsDuplicates = r.SkippedAsDuplicates,
				SkippedEmpty = r.SkippedEmpty,
				Published = r.Published,
				LastMessageId = r.LastMessageId
			})
			.ToListAsync(ct);

		return new PagedList<ParseChannelRunResponse>(items, total);
	}

	public Task<WorkerJobStateDto?> GetParseJobStateAsync(CancellationToken ct) =>
		context.WorkerJobStates
			.Where(x => x.JobName == WorkerJobNames.ParseChannels)
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
	/// Проходы источников, принадлежащих пользователю: журнал общий, чужие источники видеть нельзя
	/// </summary>
	/// <param name="userId"></param>
	/// <returns></returns>
	private IQueryable<ChannelParsingRun> UserRuns(Guid userId) =>
		context.ChannelParsingRuns.Where(r => r.ChannelParsingSetting.Schedule.UserId == userId);
}

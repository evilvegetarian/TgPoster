using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Dashboard.GetDashboard;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Storages;

internal sealed class GetDashboardStorage(PosterContext context) : IGetDashboardStorage
{
	private const int TextPreviewLength = 120;

	public async Task<List<DashboardScheduleRow>> GetSchedulesAsync(
		Guid userId,
		DateTimeOffset now,
		CancellationToken ct
	)
	{
		var since = now.AddHours(-24);

		var schedules = await context.Schedules
			.Where(s => s.UserId == userId)
			.OrderBy(s => s.Name)
			.Select(s => new
			{
				s.Id,
				s.Name,
				s.ChannelName,
				s.IsActive,
				TimePostings = s.Days.Select(d => d.TimePostings).ToList(),
				PlannedCount = s.Messages.Count(m =>
					(m.Status == MessageStatus.Register || m.Status == MessageStatus.InHandle)
					&& m.IsVerified
					&& m.TimePosting > now),
				AwaitingApprovalCount = s.Messages.Count(m =>
					m.Status == MessageStatus.Register
					&& !m.IsVerified
					&& m.TimePosting > now),
				NextPostAt = s.Messages
					.Where(m => (m.Status == MessageStatus.Register || m.Status == MessageStatus.InHandle)
					            && m.IsVerified
					            && m.TimePosting > now)
					.Min(m => (DateTimeOffset?)m.TimePosting),
				LastPlannedAt = s.Messages
					.Where(m => (m.Status == MessageStatus.Register || m.Status == MessageStatus.InHandle)
					            && m.IsVerified
					            && m.TimePosting > now)
					.Max(m => (DateTimeOffset?)m.TimePosting),
				FailedLast24h = s.Messages.Count(m => m.Status == MessageStatus.Error && m.TimePosting >= since)
			})
			.ToListAsync(ct);

		// Время публикации лежит json-колонкой, поэтому наличие слотов проверяется уже в памяти
		return schedules
			.Select(s => new DashboardScheduleRow(
				s.Id,
				s.Name,
				s.ChannelName,
				s.IsActive,
				s.TimePostings.Any(times => times.Count > 0),
				s.PlannedCount,
				s.AwaitingApprovalCount,
				s.NextPostAt,
				s.LastPlannedAt,
				s.FailedLast24h))
			.ToList();
	}

	public async Task<List<DashboardUpcomingPost>> GetUpcomingPostsAsync(
		Guid userId,
		DateTimeOffset now,
		int limit,
		CancellationToken ct
	)
	{
		var posts = await context.Messages
			.Where(m => m.Schedule.UserId == userId
			            && (m.Status == MessageStatus.Register || m.Status == MessageStatus.InHandle)
			            && m.TimePosting > now)
			.OrderBy(m => m.TimePosting)
			.Take(limit)
			.Select(m => new
			{
				m.Id,
				m.ScheduleId,
				ScheduleName = m.Schedule.Name,
				m.TimePosting,
				TextPreview = m.TextMessage == null
					? null
					: m.TextMessage.Substring(0, Math.Min(TextPreviewLength, m.TextMessage.Length)),
				PhotoCount = m.MessageFiles.Count(f => f.ParentFileId == null && f.FileType == FileTypes.Photo),
				VideoCount = m.MessageFiles.Count(f => f.ParentFileId == null && f.FileType == FileTypes.Video),
				NeedApprove = !m.IsVerified
			})
			.ToListAsync(ct);

		return posts
			.ConvertAll(x => new DashboardUpcomingPost(
				x.Id,
				x.ScheduleId,
				x.ScheduleName,
				x.TimePosting,
				NullIfEmpty(x.TextPreview),
				x.PhotoCount,
				x.VideoCount,
				x.NeedApprove));
	}

	public async Task<List<DashboardFailedPost>> GetFailedPostsAsync(
		Guid userId,
		DateTimeOffset since,
		int limit,
		CancellationToken ct
	)
	{
		var posts = await context.Messages
			.Where(m => m.Schedule.UserId == userId
			            && m.Status == MessageStatus.Error
			            && m.TimePosting >= since)
			.OrderByDescending(m => m.TimePosting)
			.Take(limit)
			.Select(m => new
			{
				m.Id,
				m.ScheduleId,
				ScheduleName = m.Schedule.Name,
				m.TimePosting,
				m.SendError,
				TextPreview = m.TextMessage == null
					? null
					: m.TextMessage.Substring(0, Math.Min(TextPreviewLength, m.TextMessage.Length))
			})
			.ToListAsync(ct);

		return posts
			.ConvertAll(x => new DashboardFailedPost(
				x.Id,
				x.ScheduleId,
				x.ScheduleName,
				x.TimePosting,
				x.SendError,
				NullIfEmpty(x.TextPreview)));
	}

	public Task<int> GetFailedPostsCountAsync(Guid userId, DateTimeOffset since, CancellationToken ct) =>
		context.Messages.CountAsync(m => m.Schedule.UserId == userId
		                                 && m.Status == MessageStatus.Error
		                                 && m.TimePosting >= since, ct);

	public async Task<DashboardRepostCounts> GetRepostCountsAsync(
		Guid userId,
		DateTimeOffset since,
		CancellationToken ct
	)
	{
		var counts = await context.Set<RepostLog>()
			.Where(x => x.Created >= since
			            && x.RepostDestination.RepostSettings.Schedule.UserId == userId)
			.GroupBy(x => x.Status)
			.Select(g => new { Status = g.Key, Count = g.Count() })
			.ToListAsync(ct);

		var byStatus = counts.ToDictionary(x => x.Status, x => x.Count);

		return new DashboardRepostCounts(
			byStatus.GetValueOrDefault(RepostStatus.Success),
			byStatus.GetValueOrDefault(RepostStatus.Failed),
			byStatus.GetValueOrDefault(RepostStatus.Skipped));
	}

	public async Task<List<DashboardRepostError>> GetRepostTopErrorsAsync(
		Guid userId,
		DateTimeOffset since,
		int limit,
		CancellationToken ct
	)
	{
		var errors = await context.Set<RepostLog>()
			.Where(x => x.Created >= since
			            && x.Status == RepostStatus.Failed
			            && x.Error != null
			            && x.Error != ""
			            && x.RepostDestination.RepostSettings.Schedule.UserId == userId)
			.GroupBy(x => x.Error)
			.Select(g => new { g.Key, Count = g.Count() })
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Key)
			.Take(limit)
			.ToListAsync(ct);

		return errors.ConvertAll(x => new DashboardRepostError(x.Key!, x.Count));
	}

	public async Task<List<DashboardSessionRow>> GetSessionsAsync(Guid userId, CancellationToken ct)
	{
		var sessions = await context.TelegramSessions
			.Where(s => s.UserId == userId)
			.OrderBy(s => s.Created)
			.Select(s => new
			{
				s.Id,
				s.Name,
				s.PhoneNumber,
				s.Status,
				s.IsActive,
				s.FloodWaitUntil
			})
			.ToListAsync(ct);

		return sessions.ConvertAll(s => new DashboardSessionRow(
			s.Id,
			s.Name,
			s.PhoneNumber,
			(Telegram.Models.TelegramSessionStatus)s.Status,
			s.IsActive,
			s.FloodWaitUntil));
	}

	public Task<List<DashboardParsingProblem>> GetParsingProblemsAsync(Guid userId, CancellationToken ct) =>
		context.ChannelParsingParameters
			.Where(x => x.Schedule.UserId == userId && x.Status == ParsingStatus.Failed)
			.OrderBy(x => x.Schedule.Name)
			.ThenBy(x => x.Channel)
			.Select(x => new DashboardParsingProblem(
				x.Id,
				x.Channel,
				x.ScheduleId,
				x.Schedule.Name,
				x.LastParseDate))
			.ToListAsync(ct);

	public async Task<List<DashboardWorker>> GetWorkersAsync(CancellationToken ct)
	{
		var workers = await context.WorkerJobStates
			.OrderBy(x => x.JobName)
			.Select(x => new
			{
				x.JobName,
				x.Status,
				x.LastFinishedAt,
				x.NextRunAt,
				x.CooldownUntil,
				x.LastError
			})
			.ToListAsync(ct);

		return workers.ConvertAll(x => new DashboardWorker(
			x.JobName,
			(WorkerJobStateStatus)x.Status,
			x.LastFinishedAt,
			x.NextRunAt,
			x.CooldownUntil,
			x.LastError));
	}

	private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}

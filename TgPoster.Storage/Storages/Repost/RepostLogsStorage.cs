using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.Models;
using TgPoster.API.Domain.UseCases.Repost.GetRepostLogsSummary;
using TgPoster.API.Domain.UseCases.Repost.ListRepostLogs;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class RepostLogsStorage(PosterContext context) : IListRepostLogsStorage, IGetRepostLogsSummaryStorage
{
	private const int PreviewLength = 200;

	public async Task<PagedList<RepostLogDto>> GetRepostLogsAsync(
		Guid userId,
		ListRepostLogsQuery query,
		CancellationToken ct
	)
	{
		var logs = Filter(
			userId,
			query.RepostSettingsId,
			query.DestinationId,
			query.MessageId,
			query.From,
			query.To);

		if (query.Status.HasValue)
		{
			logs = logs.Where(l => l.Status == query.Status.Value);
		}

		var total = await logs.CountAsync(ct);

		var items = await logs
			.OrderByDescending(l => l.Created)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.Select(l => new RepostLogDto
			{
				Id = l.Id,
				CreatedAt = l.Created!.Value,
				RepostSettingsId = l.RepostDestination.RepostSettingsId,
				ScheduleName = l.RepostDestination.RepostSettings.Schedule.Name,
				SourceChannelName = l.RepostDestination.RepostSettings.Schedule.ChannelName,
				MessageId = l.MessageId,
				MessagePreview = l.Message.TextMessage == null
					? null
					: l.Message.TextMessage.Length > PreviewLength
						? l.Message.TextMessage.Substring(0, PreviewLength)
						: l.Message.TextMessage,
				MessageTimePosting = l.Message.TimePosting,
				DestinationId = l.RepostDestinationId,
				DestinationChatId = l.RepostDestination.ChatId,
				DestinationTitle = l.RepostDestination.Title,
				DestinationUsername = l.RepostDestination.Username,
				Status = l.Status,
				Reason = l.Reason,
				TelegramMessageId = l.TelegramMessageId,
				Error = l.Error,
				RepostedAt = l.RepostedAt
			})
			.ToListAsync(ct);

		return new PagedList<RepostLogDto>(items, total);
	}

	public async Task<RepostLogsSummaryResponse> GetSummaryAsync(
		Guid userId,
		GetRepostLogsSummaryQuery query,
		CancellationToken ct
	)
	{
		var logs = Filter(
			userId,
			query.RepostSettingsId,
			query.DestinationId,
			query.MessageId,
			query.From,
			query.To);

		var grouped = await logs
			.GroupBy(l => new { l.Status, l.Reason })
			.Select(g => new
			{
				g.Key.Status,
				g.Key.Reason,
				Count = g.Count()
			})
			.ToListAsync(ct);

		var lastSuccessAt = await logs
			.Where(l => l.Status == RepostStatus.Success)
			.MaxAsync(l => l.RepostedAt, ct);

		var reasons = grouped
			.Where(x => x.Reason != RepostLogReason.None)
			.GroupBy(x => x.Reason)
			.Select(g => new RepostLogReasonCount
			{
				Reason = g.Key,
				Count = g.Sum(x => x.Count)
			})
			.OrderByDescending(x => x.Count)
			.ToList();

		return new RepostLogsSummaryResponse
		{
			Total = grouped.Sum(x => x.Count),
			Success = grouped.Where(x => x.Status == RepostStatus.Success).Sum(x => x.Count),
			Failed = grouped.Where(x => x.Status == RepostStatus.Failed).Sum(x => x.Count),
			Skipped = grouped.Where(x => x.Status == RepostStatus.Skipped).Sum(x => x.Count),
			LastSuccessAt = lastSuccessAt,
			Reasons = reasons
		};
	}

	private IQueryable<RepostLog> Filter(
		Guid userId,
		Guid? repostSettingsId,
		Guid? destinationId,
		Guid? messageId,
		DateTimeOffset? from,
		DateTimeOffset? to
	)
	{
		return context.Set<RepostLog>()
			.Where(l => l.RepostDestination.RepostSettings.Schedule.UserId == userId)
			.Where(l => repostSettingsId == null || l.RepostDestination.RepostSettingsId == repostSettingsId)
			.Where(l => destinationId == null || l.RepostDestinationId == destinationId)
			.Where(l => messageId == null || l.MessageId == messageId)
			.Where(l => from == null || l.Created >= from)
			.Where(l => to == null || l.Created <= to);
	}
}

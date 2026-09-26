using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.CrossPosting;

namespace TgPoster.Storage.Storages.CrossPosting;

/// <summary>
///     Хранилище очереди и состояний кросс-постинга
/// </summary>
internal sealed class CrossPostWorkerStorage(PosterContext context, GuidFactory guidFactory)
	: ICrossPostWorkerStorage
{
	public async Task<int> EnqueueDueAsync(DateTimeOffset now, CancellationToken ct)
	{
		var since = now.AddDays(-1);
		var candidates = await context.Messages
			.Where(m => m.Status == MessageStatus.Send && m.TelegramMessageId != null && m.CrossPostEnabled && m.TimePosting > since)
			.SelectMany(m => m.Schedule.CrossPostTargets
				.Where(t => t.IsActive
				            && t.SocialAccount.Status == SocialAccountStatus.Active
				            && t.Created <= m.TimePosting
				            && (t.IncludeParsed || m.ChannelParsingSettingId == null)
				            && !context.CrossPosts.Any(cp => cp.MessageId == m.Id && cp.SocialAccountId == t.SocialAccountId))
				.Select(t => new
				{
					MessageId = m.Id,
					TargetId = t.Id,
					t.SocialAccountId,
					t.SocialAccount.Platform,
					AccountName = t.SocialAccount.Name,
					m.TimePosting,
					t.DelayMinutes
				}))
			.ToListAsync(ct);

		var crossPosts = candidates.Select(c => new CrossPost
		{
			Id = guidFactory.New(),
			MessageId = c.MessageId,
			CrossPostTargetId = c.TargetId,
			SocialAccountId = c.SocialAccountId,
			Platform = c.Platform,
			AccountName = c.AccountName,
			Status = CrossPostStatus.Pending,
			ScheduledAt = c.TimePosting.AddMinutes(c.DelayMinutes),
			Attempts = 0
		}).ToList();

		if (crossPosts.Count == 0)
		{
			return 0;
		}

		await context.CrossPosts.AddRangeAsync(crossPosts, ct);
		await context.SaveChangesAsync(ct);

		return crossPosts.Count;
	}

	public async Task<int> FailStuckAsync(DateTimeOffset startedBefore, string error, CancellationToken ct)
	{
		var stuck = await context.CrossPosts
			.Where(cp => cp.Status == CrossPostStatus.InProgress && cp.StartedAt < startedBefore)
			.ToListAsync(ct);

		foreach (var cp in stuck)
		{
			cp.Status = CrossPostStatus.Failed;
			cp.Error = Truncate(error, 2000);
		}

		await context.SaveChangesAsync(ct);
		return stuck.Count;
	}

	public async Task<int> SkipOrphanedAsync(CancellationToken ct)
	{
		// Флаги считаются одним запросом, DbSet.Any применяет фильтр мягкого удаления сам
		var orphans = await context.CrossPosts
			.Where(cp => cp.Status == CrossPostStatus.Pending)
			.Select(cp => new
			{
				cp.Id,
				MessageExists = context.Messages.Any(m => m.Id == cp.MessageId && m.CrossPostEnabled),
				TargetExists = context.CrossPostTargets.Any(t => t.Id == cp.CrossPostTargetId && t.IsActive),
				AccountExists = context.SocialAccounts.Any(a =>
					a.Id == cp.SocialAccountId && a.Status == SocialAccountStatus.Active)
			})
			.Where(x => !x.MessageExists || !x.TargetExists || !x.AccountExists)
			.ToListAsync(ct);

		if (orphans.Count == 0)
		{
			return 0;
		}

		var reasons = orphans.ToDictionary(
			x => x.Id,
			x => !x.MessageExists
				? "Пост удалён или кросс-постинг для него выключен"
				: !x.TargetExists
					? "Настройка кросс-постинга удалена или выключена"
					: "Аккаунт соцсети удалён или требует переподключения");

		var ids = reasons.Keys.ToList();
		var crossPosts = await context.CrossPosts
			.Where(cp => ids.Contains(cp.Id))
			.ToListAsync(ct);

		foreach (var cp in crossPosts)
		{
			cp.Status = CrossPostStatus.Skipped;
			cp.Error = Truncate(reasons[cp.Id], 2000);
		}

		await context.SaveChangesAsync(ct);
		return crossPosts.Count;
	}

	public async Task<Guid?> TakeNextDueAsync(DateTimeOffset now, CancellationToken ct)
	{
		var crossPost = await context.CrossPosts
			.Where(cp => cp.Status == CrossPostStatus.Pending && cp.ScheduledAt <= now)
			.OrderBy(cp => cp.ScheduledAt)
			.FirstOrDefaultAsync(ct);

		if (crossPost is null)
		{
			return null;
		}

		crossPost.Status = CrossPostStatus.InProgress;
		crossPost.StartedAt = now;
		crossPost.Attempts++;

		await context.SaveChangesAsync(ct);
		return crossPost.Id;
	}

	public Task<CrossPostJobDto?> GetJobAsync(Guid crossPostId, CancellationToken ct)
	{
		return context.CrossPosts
			.AsNoTracking()
			.Where(cp => cp.Id == crossPostId)
			.Select(cp => new CrossPostJobDto
			{
				Id = cp.Id,
				MessageId = cp.MessageId,
				SocialAccountId = cp.SocialAccountId,
				Platform = cp.Platform,
				Attempts = cp.Attempts,
				TextMessage = cp.Message.TextMessage,
				Format = cp.Message.CrossPostFormat ?? cp.CrossPostTarget.Format,
				LinkTarget = cp.CrossPostTarget.LinkTarget,
				CustomLink = cp.CrossPostTarget.CustomLink,
				CallToAction = cp.CrossPostTarget.CallToAction,
				IncludeMedia = cp.CrossPostTarget.IncludeMedia,
				ChannelName = cp.Message.Schedule.ChannelName,
				TelegramMessageId = cp.Message.TelegramMessageId,
				BotTokenEncrypted = cp.Message.Schedule.TelegramBot.ApiTelegram,
				AccountName = cp.SocialAccount.Name,
				AccountExternalUserId = cp.SocialAccount.ExternalUserId,
				AccountSecretEncrypted = cp.SocialAccount.Secret,
				Files = cp.Message.MessageFiles
					.Where(f => f.ParentFileId == null)
					.OrderBy(f => f.Order)
					.Select(f => new CrossPostFileDto
					{
						TgFileId = f.TgFileId,
						ContentType = f.ContentType,
						Order = f.Order,
						ThumbnailTgFileId = f.Thumbnails
							.Where(x => x.FileType == FileTypes.Thumbnail)
							.OrderBy(x => x.Order)
							.Select(x => x.TgFileId)
							.FirstOrDefault()
					})
					.ToList()
			})
			.FirstOrDefaultAsync(ct);
	}

	public async Task MarkPublishedAsync(Guid id, string externalPostId, string? externalUrl, DateTimeOffset publishedAt, CancellationToken ct)
	{
		var crossPost = await context.CrossPosts.FirstAsync(cp => cp.Id == id, ct);

		crossPost.Status = CrossPostStatus.Published;
		crossPost.ExternalPostId = Truncate(externalPostId, 512);
		crossPost.ExternalUrl = Truncate(externalUrl, 1024);
		crossPost.PublishedAt = publishedAt;
		crossPost.Error = null;

		await context.SaveChangesAsync(ct);
	}

	public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct)
	{
		var crossPost = await context.CrossPosts.FirstAsync(cp => cp.Id == id, ct);

		crossPost.Status = CrossPostStatus.Failed;
		crossPost.Error = Truncate(error, 2000);

		await context.SaveChangesAsync(ct);
	}

	public async Task MarkSkippedAsync(Guid id, string reason, CancellationToken ct)
	{
		var crossPost = await context.CrossPosts.FirstAsync(cp => cp.Id == id, ct);

		crossPost.Status = CrossPostStatus.Skipped;
		crossPost.Error = Truncate(reason, 2000);

		await context.SaveChangesAsync(ct);
	}

	public async Task RescheduleAsync(Guid id, DateTimeOffset scheduledAt, string error, CancellationToken ct)
	{
		var crossPost = await context.CrossPosts.FirstAsync(cp => cp.Id == id, ct);

		crossPost.Status = CrossPostStatus.Pending;
		crossPost.ScheduledAt = scheduledAt;
		crossPost.StartedAt = null;
		crossPost.Error = Truncate(error, 2000);

		await context.SaveChangesAsync(ct);
	}

	public async Task MarkAccountNeedsReauthAsync(Guid accountId, string error, CancellationToken ct)
	{
		var account = await context.SocialAccounts.FirstAsync(a => a.Id == accountId, ct);

		account.Status = SocialAccountStatus.NeedsReauth;
		account.LastError = Truncate(error, 1000);

		await context.SaveChangesAsync(ct);
	}

	private static string? Truncate(string? value, int maxLength)
	{
		if (value is null || value.Length <= maxLength)
		{
			return value;
		}

		return value[..maxLength];
	}
}

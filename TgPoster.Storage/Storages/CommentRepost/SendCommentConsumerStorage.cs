using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

namespace TgPoster.Storage.Storages.CommentRepost;

internal sealed class SendCommentConsumerStorage(PosterContext context, GuidFactory guidFactory)
	: ISendCommentConsumerStorage
{
	public async Task CreateLogAsync(
		Guid settingsId,
		int originalPostId,
		int? forwardedMessageId,
		int? commentMessageId,
		string? error,
		CancellationToken ct
	)
	{
		var log = new CommentRepostLog
		{
			Id = guidFactory.New(),
			CommentRepostSettingsId = settingsId,
			OriginalPostId = originalPostId,
			ForwardedMessageId = forwardedMessageId,
			CommentMessageId = commentMessageId,
			Status = error is null ? RepostStatus.Success : RepostStatus.Failed,
			Error = error,
			SentAt = DateTime.UtcNow
		};

		context.CommentRepostLogs.Add(log);
		await context.SaveChangesAsync(ct);
	}

	public Task<bool> IsAccountBannedAsync(Guid telegramSessionId, long chatId, CancellationToken ct)
	{
		return context.TelegramSessionChatBans
			.AnyAsync(x => x.TelegramSessionId == telegramSessionId && x.ChatId == chatId, ct);
	}

	public async Task MarkAccountBannedAsync(
		Guid telegramSessionId,
		long discussionGroupId,
		string? error,
		CancellationToken ct
	)
	{
		await context.AddOrUpdateChatBanAsync(guidFactory, telegramSessionId, discussionGroupId, error, ct);

		var settings = await context.CommentRepostSettings
			.Where(x => x.TelegramSessionId == telegramSessionId
			            && x.DiscussionGroupId == discussionGroupId
			            && x.IsActive)
			.ToListAsync(ct);

		foreach (var setting in settings)
		{
			setting.IsActive = false;
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task MarkChatWriteForbiddenAsync(Guid settingsId, long discussionGroupId, CancellationToken ct)
	{
		var settings = await context.CommentRepostSettings.FirstAsync(x => x.Id == settingsId, ct);
		settings.IsActive = false;

		// Группы обсуждения обычно нет в Discover — тогда записывать запрет некуда
		var channels = await context.DiscoveredChannels
			.IgnoreQueryFilters()
			.Where(x => x.TelegramId == discussionGroupId)
			.ToListAsync(ct);

		foreach (var channel in channels)
		{
			channel.CanSendMessages = false;
		}

		await context.SaveChangesAsync(ct);
	}
}

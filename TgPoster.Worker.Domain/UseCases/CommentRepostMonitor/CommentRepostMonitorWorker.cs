using Hangfire;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgPoster.Telegram;
using TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

namespace TgPoster.Worker.Domain.UseCases.CommentRepostMonitor;

public class CommentRepostMonitorWorker(
	ICommentRepostMonitorStorage storage,
	ITelegramMessageService tgMessages,
	IPublishEndpoint publishEndpoint,
	ILogger<CommentRepostMonitorWorker> logger,
	IHostApplicationLifetime lifetime)
{
	[DisableConcurrentExecution(60)]
	public async Task CheckForNewPostsAsync()
	{
		var settings = await storage.GetActiveSettingsAsync(lifetime.ApplicationStopping);
		if (settings.Count == 0)
		{
			return;
		}

		foreach (var setting in settings)
		{
			try
			{
				await ProcessSettingAsync(setting);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Ошибка при проверке канала {ChannelId} для настройки {Id}",
					setting.WatchedChannelId, setting.Id);
			}
		}
	}

	private async Task ProcessSettingAsync(CommentRepostSettingDto setting)
	{
		var peer = TelegramPeer.Channel(setting.WatchedChannelId, setting.WatchedChannelAccessHash ?? 0);
		var historyResult = await tgMessages.GetHistoryAsync(
			setting.TelegramSessionId,
			peer,
			limit: 20,
			minId: setting.LastProcessedPostId ?? 0,
			ct: lifetime.ApplicationStopping);

		if (!historyResult.IsSuccess)
		{
			logger.LogWarning(
				"Не удалось получить историю канала {ChannelId} (статус {Status}): {Error}",
				setting.WatchedChannelId, historyResult.Status, historyResult.ErrorMessage);
			return;
		}

		var newPosts = historyResult.Value!.Messages
			.Where(m => m.Id > (setting.LastProcessedPostId ?? 0))
			.OrderBy(m => m.Id)
			.ToList();

		if (newPosts.Count == 0)
		{
			return;
		}

		logger.LogDebug("Найдено {Count} новых постов в канале {ChannelId}",
			newPosts.Count, setting.WatchedChannelId);

		foreach (var post in newPosts)
		{
			var command = new SendCommentCommand
			{
				CommentRepostSettingsId = setting.Id,
				OriginalPostId = post.Id,
				WatchedChannelId = setting.WatchedChannelId,
				WatchedChannelAccessHash = setting.WatchedChannelAccessHash,
				DiscussionGroupId = setting.DiscussionGroupId,
				DiscussionGroupAccessHash = setting.DiscussionGroupAccessHash,
				SourceChannelId = setting.SourceChannelId,
				TelegramSessionId = setting.TelegramSessionId
			};

			await publishEndpoint.Publish(command, lifetime.ApplicationStopping);
		}

		var maxPostId = newPosts.Max(m => m.Id);
		await storage.UpdateLastProcessedAsync(setting.Id, maxPostId, lifetime.ApplicationStopping);
	}
}

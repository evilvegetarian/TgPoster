using Hangfire;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Telegram;
using TgPoster.Worker.Domain.UseCases.SendCommentConsumer;
using TL;

namespace TgPoster.Worker.Domain.UseCases.CommentRepostMonitor;

public class CommentRepostMonitorWorker(
	ICommentRepostMonitorStorage storage,
	ITelegramAuthService authService,
	IPublishEndpoint publishEndpoint,
	ILogger<CommentRepostMonitorWorker> logger,
	IHostApplicationLifetime lifetime)
{
	[DisableConcurrentExecution(60)]
	public async Task CheckForNewPostsAsync()
	{
		logger.LogInformation("Начали проверку новых постов для комментирующего репоста");

		var settings = await storage.GetActiveSettingsAsync(lifetime.ApplicationStopping);
		if (settings.Count == 0)
		{
			logger.LogInformation("Нет активных настроек комментирующего репоста");
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
		var client = await authService.GetClientAsync(setting.TelegramSessionId, lifetime.ApplicationStopping);

		var peer = new InputPeerChannel(setting.WatchedChannelId, setting.WatchedChannelAccessHash ?? 0);
		var history = await client.Messages_GetHistory(
			peer,
			limit: 20,
			min_id: setting.LastProcessedPostId ?? 0);

		var newPosts = history.Messages
			.OfType<TL.Message>()
			.Where(m => m.ID > (setting.LastProcessedPostId ?? 0))
			.OrderBy(m => m.ID)
			.ToList();

		if (newPosts.Count == 0)
			return;

		logger.LogInformation("Найдено {Count} новых постов в канале {ChannelId}",
			newPosts.Count, setting.WatchedChannelId);

		foreach (var post in newPosts)
		{
			var command = new SendCommentCommand
			{
				CommentRepostSettingsId = setting.Id,
				OriginalPostId = post.ID,
				WatchedChannelId = setting.WatchedChannelId,
				WatchedChannelAccessHash = setting.WatchedChannelAccessHash,
				DiscussionGroupId = setting.DiscussionGroupId,
				DiscussionGroupAccessHash = setting.DiscussionGroupAccessHash,
				SourceChannelId = setting.SourceChannelId,
				TelegramSessionId = setting.TelegramSessionId
			};

			await publishEndpoint.Publish(command, lifetime.ApplicationStopping);
		}

		var maxPostId = newPosts.Max(m => m.ID);
		await storage.UpdateLastProcessedAsync(setting.Id, maxPostId, lifetime.ApplicationStopping);
	}
}

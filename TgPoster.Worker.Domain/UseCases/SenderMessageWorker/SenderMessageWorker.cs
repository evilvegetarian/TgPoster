using System.Diagnostics.CodeAnalysis;
using Hangfire;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Security.Cryptography;
using Shared.Telegram;
using Shared.Utilities;
using Shared.YouTube;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

public class SenderMessageWorker(
	ISenderMessageStorage storage,
	ILogger<SenderMessageWorker> logger,
	ICryptoAES crypto,
	TelegramOptions options,
	YouTubeService youTubeService,
	IPublishEndpoint publishEndpoint,
	TelegramBotManager botManager,
	IHostApplicationLifetime lifetime,
	TelegramExecuteServices telegramExecuteServices
)
{
	public async Task ProcessMessagesAsync()
	{
		var messageDetails = await storage.GetMessagesAsync();
		var messages = messageDetails.SelectMany(x => x.MessageDto).ToList();
		if (messages.Count == 0)
		{
			return;
		}

		await storage.UpdateStatusInHandleMessageAsync(messages.Select(x => x.Id).ToList());
		logger.LogDebug("Найдено {count} сообщений", messages.Count);
		foreach (var detail in messageDetails)
		{
			var token = crypto.Decrypt(options.SecretKey, detail.Api);

			foreach (var message in detail.MessageDto.OrderBy(x => x.TimePosting))
			{
				try
				{
					BackgroundJob.Schedule<SenderMessageWorker>(
						x => x.SendMessageAsync(message.Id, token, detail.ChannelId, message, detail.YouTubeAccount),
						message.TimePosting);
					logger.LogDebug(
						"Сообщение для чата {channelId} запланировано на {timePosting} сек.", detail.ChannelId,
						message.TimePosting);
				}
				catch (Exception e)
				{
					logger.LogError(e, "Ошибка во время отправки сообщения {id}", message.Id);
					await storage.UpdateSendStatusMessageAsync(message.Id);
				}
			}
		}
	}

	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public async Task SendMessageAsync(
		Guid messageId,
		string token,
		long chatId,
		MessageDto message,
		YouTubeAccountDto? youTubeAccount
	)
	{
		var ct = lifetime.ApplicationStopping;
		var bot = botManager.GetClient(token);
		var medias = message.File.Select(file => (InputMedia)(file.ContentType.GetFileType() == FileTypes.Image
				? new InputMediaPhoto(file.TgFileId)
				: new InputMediaVideo(file.TgFileId)))
			.ToList();

		int? telegramMessageId;

		if (medias.Any())
		{
			var captionText = message.Message ?? string.Empty;
			var isCaptionTooLong = captionText.Length > 1024;

			if (!string.IsNullOrWhiteSpace(captionText) && !isCaptionTooLong)
			{
				medias[0].Caption = captionText;
				medias[0].ParseMode = ParseMode.Html;
			}

			var result = await telegramExecuteServices.SendMediaGroupAsync(bot, chatId, medias.Select(x => (IAlbumInputMedia)x), ct);
			if (!result.IsSuccess)
			{
				await storage.UpdateErrorStatusMessageAsync(messageId, ct);
				return;
			}

			telegramMessageId = result.MessageId;

			if (!string.IsNullOrWhiteSpace(captionText) && isCaptionTooLong)
			{
				var captionResult = await telegramExecuteServices.SendTextAsync(bot, chatId, captionText, ct);
				if (!captionResult.IsSuccess)
					logger.LogWarning("Не удалось отправить подпись к медиа-группе для сообщения {MessageId}", messageId);
			}
		}
		else
		{
			var result = await telegramExecuteServices.SendTextAsync(bot, chatId, message.Message!, ct);
			if (!result.IsSuccess)
			{
				await storage.UpdateErrorStatusMessageAsync(messageId, ct);
				return;
			}

			telegramMessageId = result.MessageId;
		}

		await storage.UpdateSendStatusMessageAsync(messageId);
		logger.LogDebug("Отправлено сообщение в чат {chatId}", chatId);

		if (telegramMessageId.HasValue)
		{
			await storage.SaveTelegramMessageIdAsync(messageId, telegramMessageId.Value, ct);
			logger.LogDebug("Сохранен TelegramMessageId: {TelegramMessageId} для сообщения {MessageId}",
				telegramMessageId.Value, messageId);

			var repostSettingsList = await storage.GetRepostSettingsForMessageAsync(messageId, ct);
			foreach (var repostSettings in repostSettingsList.Where(rs => rs.Destinations.Count > 0))
			{
				var command = new RepostMessageCommand
				{
					MessageId = messageId,
					ScheduleId = repostSettings.ScheduleId,
					RepostSettingsId = repostSettings.Id
				};

				await publishEndpoint.Publish(command, ct);
				logger.LogDebug(
					"Опубликовано событие репоста для сообщения {MessageId} в {Count} направлений",
					messageId, repostSettings.Destinations.Count);
			}
		}

		if (youTubeAccount?.AutoPostingVideo == true)
		{
			logger.LogDebug("Начал отправку видео в ютуб");
			await UploadVideosToYouTubeAsync(bot, message, youTubeAccount);
		}
	}

	private async Task UploadVideosToYouTubeAsync(
		ITelegramBotClient bot,
		MessageDto message,
		YouTubeAccountDto youTubeAccount
	)
	{
		var videoFiles = message.File
			.Where(f => f.ContentType.GetFileType() == FileTypes.Video)
			.ToList();

		if (videoFiles.Count == 0)
		{
			logger.LogDebug("Нет видео файлов для загрузки на YouTube");
			return;
		}

		foreach (var videoFile in videoFiles)
		{
			try
			{
				using var stream = new MemoryStream();
				await bot.GetInfoAndDownloadFile(videoFile.TgFileId, stream, lifetime.ApplicationStopping);

				var result = await youTubeService.UploadVideoAsync(youTubeAccount, stream);

				await storage.UpdateYouTubeTokensAsync(youTubeAccount.Id, result.AccessToken, result.RefreshToken,
					lifetime.ApplicationStopping);

				logger.LogDebug("Видео успешно загружено на YouTube с названием: {title}",
					youTubeAccount.DefaultTitle);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Ошибка при загрузке видео на YouTube");
			}
		}
	}
}

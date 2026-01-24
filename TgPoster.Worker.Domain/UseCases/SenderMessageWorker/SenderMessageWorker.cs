using System.Diagnostics.CodeAnalysis;
using Hangfire;
using MassTransit;
using Microsoft.Extensions.Logging;
using Security.Cryptography;
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
	IPublishEndpoint publishEndpoint
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
		logger.LogInformation("Найдено {count} сообщений", messages.Count);
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
					logger.LogInformation(
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
		var bot = new TelegramBotClient(token);
		var medias = new List<InputMedia>();

		foreach (var file in message.File)
		{
			if (file.ContentType.GetFileType() == FileTypes.Image)
			{
				medias.Add(new InputMediaPhoto(file.TgFileId));
			}
			else if (file.ContentType.GetFileType() == FileTypes.Video)
			{
				medias.Add(new InputMediaVideo(file.TgFileId));
			}
		}

		int? telegramMessageId = null;

		if (medias.Any())
		{
			var captionText = message.Message ?? string.Empty;

			var isCaptionTooLong = captionText.Length > 1024;

			if (!string.IsNullOrWhiteSpace(captionText) && !isCaptionTooLong)
			{
				medias[0].Caption = captionText;
				medias[0].ParseMode = ParseMode.Html;
				var messages = await bot.SendMediaGroup(chatId, medias.Select(x => (IAlbumInputMedia)x));
				telegramMessageId = messages.FirstOrDefault()?.MessageId;
			}
			else
			{
				var messages = await bot.SendMediaGroup(chatId, medias.Select(x => (IAlbumInputMedia)x));
				telegramMessageId = messages.FirstOrDefault()?.MessageId;

				if (!string.IsNullOrWhiteSpace(captionText))
				{
					await bot.SendMessage(chatId, captionText);
				}
			}
		}
		else
		{
			var sentMessage = await bot.SendMessage(chatId, message.Message!);
			telegramMessageId = sentMessage.MessageId;
		}

		await storage.UpdateSendStatusMessageAsync(messageId);
		logger.LogInformation("Отправлено сообщение в чат {chatId}", chatId);

		if (telegramMessageId.HasValue)
		{
			await storage.SaveTelegramMessageIdAsync(messageId, telegramMessageId.Value, CancellationToken.None);
			logger.LogInformation("Сохранен TelegramMessageId: {TelegramMessageId} для сообщения {MessageId}",
				telegramMessageId.Value, messageId);

			var repostSettings = await storage.GetRepostSettingsForMessageAsync(messageId, CancellationToken.None);
			if (repostSettings != null && repostSettings.Destinations.Count > 0)
			{
				var command = new RepostMessageCommand
				{
					MessageId = messageId,
					ScheduleId = repostSettings.ScheduleId
				};

				await publishEndpoint.Publish(command, CancellationToken.None);
				logger.LogInformation(
					"Опубликовано событие репоста для сообщения {MessageId} в {Count} направлений",
					messageId, repostSettings.Destinations.Count);
			}
		}

		if (youTubeAccount?.AutoPostingVideo == true)
		{
			logger.LogInformation("Начал отправку видео в ютуб");
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
			logger.LogInformation("Нет видео файлов для загрузки на YouTube");
			return;
		}

		foreach (var videoFile in videoFiles)
		{
			try
			{
				using var stream = new MemoryStream();
				await bot.GetInfoAndDownloadFile(videoFile.TgFileId, stream, CancellationToken.None);

				var result = await youTubeService.UploadVideoAsync(youTubeAccount, stream);

				await storage.UpdateYouTubeTokensAsync(youTubeAccount.Id, result.AccessToken, result.RefreshToken,
					CancellationToken.None);

				logger.LogInformation("Видео успешно загружено на YouTube с названием: {title}",
					youTubeAccount.DefaultTitle);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Ошибка при загрузке видео на YouTube");
			}
		}
	}
}
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
			// Длинный текст поста и без подписи уходит отдельным сообщением, поэтому лимит выбирается
			// по самому тексту: подпись не должна превращать такой пост в обрезанный caption
			var isCaptionTooLong = (message.Message?.Length ?? 0) > TelegramLimits.CaptionLength;
			var limit = isCaptionTooLong ? TelegramLimits.MessageLength : TelegramLimits.CaptionLength;
			var captionText = PostSignatureComposer.Compose(message.Message, message.Signature, limit);

			if (!string.IsNullOrWhiteSpace(captionText) && !isCaptionTooLong)
			{
				medias[0].Caption = captionText;
				medias[0].ParseMode = ParseMode.Html;
			}

			var result =
				await telegramExecuteServices.SendMediaGroupAsync(bot, chatId, medias.Select(x => (IAlbumInputMedia)x),
					ct);
			if (!result.IsSuccess)
			{
				await storage.UpdateErrorStatusMessageAsync(messageId, ct);
				return;
			}

			telegramMessageId = result.MessageId;

			if (!string.IsNullOrWhiteSpace(captionText) && isCaptionTooLong)
			{
				var captionResult =
					await telegramExecuteServices.SendTextAsync(bot, chatId, captionText, ct, ParseMode.Html);
				if (!captionResult.IsSuccess)
					logger.LogWarning("Не удалось отправить подпись к медиа-группе для сообщения {MessageId}",
						messageId);
			}
		}
		else
		{
			var text = PostSignatureComposer.Compose(message.Message, message.Signature,
				TelegramLimits.MessageLength);
			var result = await telegramExecuteServices.SendTextAsync(bot, chatId, text, ct, ParseMode.Html);
			if (!result.IsSuccess)
			{
				await storage.UpdateErrorStatusMessageAsync(messageId, ct);
				return;
			}

			telegramMessageId = result.MessageId;
		}

		await storage.UpdateSendStatusMessageAsync(messageId);

		if (telegramMessageId.HasValue)
		{
			await storage.SaveTelegramMessageIdAsync(messageId, telegramMessageId.Value, ct);

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
			}
		}

		if (youTubeAccount?.AutoPostingVideo == true)
		{
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
			}
			catch (Exception e)
			{
				logger.LogError(e, "Ошибка при загрузке видео на YouTube");
			}
		}
	}
}
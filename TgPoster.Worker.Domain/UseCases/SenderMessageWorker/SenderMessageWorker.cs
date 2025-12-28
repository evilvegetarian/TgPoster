using System.Diagnostics.CodeAnalysis;
using Hangfire;
using Microsoft.Extensions.Logging;
using Security.Interfaces;
using Shared;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgPoster.Worker.Domain.ConfigModels;

namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

public class SenderMessageWorker(
	ISenderMessageStorage storage,
	ILogger<SenderMessageWorker> logger,
	ICryptoAES crypto,
	TelegramOptions options
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
						x => x.SendMessageAsync(message.Id, token, detail.ChannelId, message),
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
		MessageDto message
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

		if (medias.Any())
		{
			var captionText = message.Message ?? string.Empty;

			var isCaptionTooLong = captionText.Length > 1024;

			if (!string.IsNullOrWhiteSpace(captionText) && !isCaptionTooLong)
			{
				medias[0].Caption = captionText;
				medias[0].ParseMode = ParseMode.Html;
				await bot.SendMediaGroup(chatId, medias.Select(x => (IAlbumInputMedia)x));
			}
			else
			{
				await bot.SendMediaGroup(chatId, medias.Select(x => (IAlbumInputMedia)x));

				if (!string.IsNullOrWhiteSpace(captionText))
				{
					await bot.SendMessage(chatId, captionText);
				}
			}
		}
		else
		{
			await bot.SendMessage(chatId, message.Message!);
		}

		await storage.UpdateSendStatusMessageAsync(messageId);
		logger.LogInformation("Отправлено сообщение в чат {chatId}", chatId);
	}
}
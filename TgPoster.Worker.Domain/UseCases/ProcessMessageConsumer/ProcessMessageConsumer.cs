using MassTransit;
using Microsoft.Extensions.Logging;
using Security.Cryptography;
using Shared.Services;
using Shared.Telegram;
using Shared.Video;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.ParseChannel;
using TL;
using Document = TL.Document;
using InputMediaPhoto = Telegram.Bot.Types.InputMediaPhoto;
using Message = TL.Message;
using TelegramBotClient = Telegram.Bot.TelegramBotClient;

namespace TgPoster.Worker.Domain.UseCases.ProcessMessageConsumer;

internal sealed class ProcessMessageConsumer(
	VideoService videoService,
	ICryptoAES cryptoAes,
	TelegramOptions telegramOptions,
	TelegramExecuteServices telegramExecuteServices,
	TimePostingService timePostingService,
	IProcessMessageConsumerStorage storage,
	ITelegramAuthService authService,
	TelegramBotManager botManager,
	ILogger<ProcessMessageConsumer> logger
) : IConsumer<ProcessMessage>
{
	public async Task Consume(ConsumeContext<ProcessMessage> context)
	{
		var command = context.Message;

		var parameters = await storage.GetChannelParsingParametersAsync(command.Id, context.CancellationToken);
		var channelName = parameters!.ChannelName;
		var chatId = parameters.ChatId;
		var isNeedVerified = parameters.IsNeedVerified;
		var scheduleId = parameters.ScheduleId;
		var deleteMedia = parameters.DeleteMedia;
		var deleteText = parameters.DeleteText;
		var encryptedToken = parameters.Token;
		var useAi = parameters.UseAi
		            && string.IsNullOrWhiteSpace(parameters.TokenOpenRouter)
		            && string.IsNullOrWhiteSpace(parameters.ModelOpenRouter);

		var ct = context.CancellationToken;
		logger.LogInformation("Начата обработка поста для ScheduleId: {ScheduleId}", scheduleId);

		var token = cryptoAes.Decrypt(telegramOptions.SecretKey, encryptedToken);
		var telegramBot = botManager.GetClient(token);

		var client = await authService.GetClientAsync(parameters.TelegramSessionId, ct);

		var resolveResult = await client.Contacts_ResolveUsername(channelName);
		if (resolveResult.Chat is not Channel channel)
		{
			logger.LogError("Не удалось найти канал по имени: {ChannelName}", channelName);
			return;
		}

		var messageDto = new MessageDto
		{
			IsNeedVerified = isNeedVerified,
			ScheduleId = scheduleId,
			ChannelParsingSettingId = command.Id
		};
		var list = new List<MediaStreamDto>();
		try
		{
			foreach (var part in command.Messages)
			{
				var files = new MediaStreamDto();

				await Task.Delay(10000, ct);
				var refreshedMessages =
					await client.Channels_GetMessages(new InputChannel(channel.ID, channel.access_hash), part.Id);
				if (refreshedMessages.Messages.FirstOrDefault() is not Message message)
				{
					continue;
				}

				var stream = new MemoryStream();

				try
				{
					if (!deleteMedia)
					{
						if (part.IsPhoto && message is { media: MessageMediaPhoto { photo: Photo photo } })
						{
							await client.DownloadFileAsync(photo, stream);

							stream.Position = 0;
							files.Photo = stream;

							var photoMessage = await telegramExecuteServices.SendPhoto(telegramBot, chatId,
								new InputFileStream(stream), 3, ct);
							var photoId = photoMessage.Photo?
								.OrderByDescending(x => x.FileSize)
								.Select(x => x.FileId)
								.FirstOrDefault();
							if (photoId is not null)
							{
								messageDto.Media.Add(new MediaDto { FileId = photoId, MimeType = "image/jpeg" });
							}

							await telegramBot.DeleteMessage(chatId, photoMessage.MessageId, ct);
						}
						else if (part.IsVideo && message is { media: MessageMediaDocument { document: Document doc } })
						{
							await client.DownloadFileAsync(doc, stream);

							stream.Position = 0;

							var inputFile = new InputFileStream(stream, "video.mp4");
							var previews = await videoService.ExtractScreenshotsAsync(stream, 3);
							files.PreviewPhoto = previews;
							List<IAlbumInputMedia> album = [new InputMediaVideo(inputFile)];
							album.AddRange(
								previews.Select<MemoryStream, InputMediaPhoto>(preview =>
									new InputMediaPhoto(preview)));

							var messages = await telegramExecuteServices.SendMedia(telegramBot, chatId, album, 3, ct);

							var previewPhotoIds = messages
								.Select(m => m.Photo?
									.OrderByDescending(x => x.FileSize)
									.Select(x => x.FileId)
									.FirstOrDefault())
								.Where(x => x != null)
								.Distinct().ToList();
							var videoId = messages.Select(m => m.Video?.FileId).FirstOrDefault(v => v != null);

							foreach (var mess in messages)
							{
								await telegramBot.DeleteMessage(chatId, mess.MessageId, ct);
							}

							if (videoId is not null)
							{
								messageDto.Media.Add(new MediaDto
								{
									MimeType = doc.mime_type,
									FileId = videoId,
									PreviewPhotoIds = previewPhotoIds!
								});
							}
						}
					}

					list.Add(files);
				}
				finally
				{
					await stream.DisposeAsync();
				}

				messageDto.Text = !deleteText && !string.IsNullOrEmpty(message.message) ? message.message : null;
				if (useAi)
				{
					//openRouterClient.SendMessageAsync()
				}
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "Ошибка во время обработки медиа для ScheduleId: {ScheduleId}", scheduleId);
			throw;
		}

		if (messageDto.Media.Count > 0 || !string.IsNullOrEmpty(messageDto.Text))
		{
			var lastMessageTimePosting = await storage.GeLastMessageTimePostingAsync(scheduleId, ct);
			var scheduleTime = await storage.GetScheduleTimeAsync(scheduleId, ct);
			var postingTime = timePostingService.GetTimeForPosting(1, scheduleTime, lastMessageTimePosting);
			messageDto.TimePosting = postingTime.First();
			await storage.CreateMessageAsync(messageDto, ct);
			logger.LogInformation("Пост для ScheduleId {ScheduleId} успешно обработан и сохранен.", scheduleId);
		}
		else
		{
			logger.LogWarning("Пост для ScheduleId {ScheduleId} оказался пустым после обработки и не был сохранен.",
				scheduleId);
		}
	}
}
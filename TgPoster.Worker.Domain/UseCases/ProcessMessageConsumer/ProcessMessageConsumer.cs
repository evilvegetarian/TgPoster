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
using InputMediaVideo = Telegram.Bot.Types.InputMediaVideo;
using Message = TL.Message;

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
		logger.LogDebug("Начата обработка поста для ScheduleId: {ScheduleId}", scheduleId);

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

		var downloadedMedia = new List<DownloadedMedia>();
		try
		{
			foreach (var part in command.Messages)
			{
				await Task.Delay(2000, ct);
				var refreshedMessages =
					await client.Channels_GetMessages(new InputChannel(channel.ID, channel.access_hash), part.Id);
				if (refreshedMessages.Messages.FirstOrDefault() is not Message message)
				{
					continue;
				}

				if (!deleteMedia)
				{
					if (part.IsPhoto && message is { media: MessageMediaPhoto { photo: Photo photo } })
					{
						var stream = new MemoryStream();
						await client.DownloadFileAsync(photo, stream);
						stream.Position = 0;
						downloadedMedia.Add(new DownloadedMedia
						{
							Stream = stream, IsPhoto = true, MimeType = "image/jpeg"
						});
					}
					else if (part.IsVideo && message is { media: MessageMediaDocument { document: Document doc } })
					{
						var stream = new MemoryStream();
						await client.DownloadFileAsync(doc, stream);
						stream.Position = 0;
						downloadedMedia.Add(new DownloadedMedia
						{
							Stream = stream, IsVideo = true, MimeType = doc.mime_type
						});
					}
				}

				messageDto.Text = !deleteText && !string.IsNullOrEmpty(message.message) ? message.message : null;
				if (useAi)
				{
					//openRouterClient.SendMessageAsync()
				}
			}

			var messagesToDelete = new List<int>();

			if (!deleteMedia && downloadedMedia.Count > 0)
			{
				await Task.Delay(3000, ct);

				var photos = downloadedMedia.Where(m => m.IsPhoto).ToList();
				var videos = downloadedMedia.Where(m => m.IsVideo).ToList();

				if (photos.Count > 1)
				{
					List<IAlbumInputMedia> album = photos
						.Select(p => (IAlbumInputMedia)new InputMediaPhoto(new InputFileStream(p.Stream)))
						.ToList();

					var botMessages = await telegramExecuteServices.SendMedia(
						telegramBot, chatId, album, 5, ct);

					foreach (var msg in botMessages)
					{
						var photoId = msg.Photo?
							.OrderByDescending(x => x.FileSize)
							.Select(x => x.FileId)
							.FirstOrDefault();
						if (photoId is not null)
						{
							messageDto.Media.Add(new MediaDto { FileId = photoId, MimeType = "image/jpeg" });
						}

						messagesToDelete.Add(msg.MessageId);
					}
				}
				else if (photos.Count == 1)
				{
					var photo = photos[0];
					var photoMessage = await telegramExecuteServices.SendPhoto(
						telegramBot, chatId, new InputFileStream(photo.Stream), 5, ct);

					var photoId = photoMessage.Photo?
						.OrderByDescending(x => x.FileSize)
						.Select(x => x.FileId)
						.FirstOrDefault();
					if (photoId is not null)
					{
						messageDto.Media.Add(new MediaDto { FileId = photoId, MimeType = "image/jpeg" });
					}

					messagesToDelete.Add(photoMessage.MessageId);
				}

				foreach (var video in videos)
				{
					if (photos.Count > 0 || videos.IndexOf(video) > 0)
					{
						await Task.Delay(2000, ct);
					}

					video.Stream.Position = 0;
					var previews = await videoService.ExtractScreenshotsAsync(video.Stream, 3);
					video.Stream.Position = 0;

					var inputFile = new InputFileStream(video.Stream, "video.mp4");
					List<IAlbumInputMedia> album = [new InputMediaVideo(inputFile)];
					album.AddRange(
						previews.Select<MemoryStream, InputMediaPhoto>(preview =>
							new InputMediaPhoto(preview)));

					var botMessages = await telegramExecuteServices.SendMedia(
						telegramBot, chatId, album, 5, ct);

					var previewPhotoIds = botMessages
						.Select(m => m.Photo?
							.OrderByDescending(x => x.FileSize)
							.Select(x => x.FileId)
							.FirstOrDefault())
						.Where(x => x != null)
						.Distinct().ToList();
					var videoId = botMessages.Select(m => m.Video?.FileId).FirstOrDefault(v => v != null);

					messagesToDelete.AddRange(botMessages.Select(m => m.MessageId));

					if (videoId is not null)
					{
						messageDto.Media.Add(new MediaDto
						{
							MimeType = video.MimeType,
							FileId = videoId,
							PreviewPhotoIds = previewPhotoIds!
						});
					}

					foreach (var preview in previews)
					{
						await preview.DisposeAsync();
					}
				}

				foreach (var msgId in messagesToDelete)
				{
					await telegramBot.DeleteMessage(chatId, msgId, ct);
				}
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "Ошибка во время обработки медиа для ScheduleId: {ScheduleId}", scheduleId);
			throw;
		}
		finally
		{
			foreach (var media in downloadedMedia)
			{
				await media.DisposeAsync();
			}
		}

		if (messageDto.Media.Count > 0 || !string.IsNullOrEmpty(messageDto.Text))
		{
			var lastMessageTimePosting = await storage.GeLastMessageTimePostingAsync(scheduleId, ct);
			var scheduleTime = await storage.GetScheduleTimeAsync(scheduleId, ct);
			var postingTime = timePostingService.GetTimeForPosting(1, scheduleTime, lastMessageTimePosting);
			messageDto.TimePosting = postingTime.First();
			await storage.CreateMessageAsync(messageDto, ct);
			logger.LogDebug("Пост для ScheduleId {ScheduleId} успешно обработан и сохранен.", scheduleId);
		}
		else
		{
			logger.LogWarning("Пост для ScheduleId {ScheduleId} оказался пустым после обработки и не был сохранен.",
				scheduleId);
		}
	}

	private sealed class DownloadedMedia : IAsyncDisposable
	{
		public required MemoryStream Stream { get; init; }
		public bool IsPhoto { get; init; }
		public bool IsVideo { get; init; }
		public required string MimeType { get; init; }

		public ValueTask DisposeAsync() => Stream.DisposeAsync();
	}
}

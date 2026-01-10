using System.Diagnostics.CodeAnalysis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
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
			logger.LogInformation("Нет видео файлов для загрузки на YouTube");
			return;
		}

		foreach (var videoFile in videoFiles)
		{
			try
			{
				using var stream = new MemoryStream();
				await bot.GetInfoAndDownloadFile(videoFile.TgFileId, stream, CancellationToken.None);

				var title = !string.IsNullOrWhiteSpace(youTubeAccount.DefaultTitle)
					? youTubeAccount.DefaultTitle
					: "Видео";

				var description = !string.IsNullOrWhiteSpace(youTubeAccount.DefaultDescription)
					? youTubeAccount.DefaultDescription
					: string.Empty;

				var tags = !string.IsNullOrWhiteSpace(youTubeAccount.DefaultTags)
					? youTubeAccount.DefaultTags
					: "shorts,vertical";

				await UploadVideoAsync(youTubeAccount, stream, title, description, tags);
				logger.LogInformation("Видео успешно загружено на YouTube с названием: {title}", title);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Ошибка при загрузке видео на YouTube");
			}
		}
	}

	private async Task<string> UploadVideoAsync(
		YouTubeAccountDto account,
		MemoryStream stream,
		string title,
		string description,
		string tags
	)
	{
		var tokenResponse = new TokenResponse
		{
			AccessToken = account.AccessToken,
			RefreshToken = account.RefreshToken
		};

		var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
		{
			ClientSecrets = new ClientSecrets
			{
				ClientId = account.ClientId,
				ClientSecret = account.ClientSecret
			}
		});

		var credential = new UserCredential(flow, "user", tokenResponse);

		var youtubeService = new YouTubeService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = "TgPoster"
		});

		var tagArray = tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

		var video = new Google.Apis.YouTube.v3.Data.Video
		{
			Snippet = new VideoSnippet
			{
				Title = title,
				Description = description,
				Tags = tagArray,
				CategoryId = "22"
			},
			Status = new VideoStatus { PrivacyStatus = "public" }
		};

		var videoId = "";
		var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", stream, "video/*");

		videosInsertRequest.ResponseReceived += v =>
		{
			videoId = v.Id;
		};

		var result = await videosInsertRequest.UploadAsync();
		if (result.Status == UploadStatus.Failed)
		{
			throw result.Exception;
		}

		return videoId;
	}
}
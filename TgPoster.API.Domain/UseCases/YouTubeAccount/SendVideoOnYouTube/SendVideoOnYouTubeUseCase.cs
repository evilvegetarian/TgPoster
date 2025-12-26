using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MediatR;
using Security.Interfaces;
using Shared;
using Telegram.Bot;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;

internal class SendVideoOnYouTubeUseCase(
	ISendVideoOnYouTubeStorage storage,
	IIdentityProvider provider,
	TelegramTokenService tokenService)
	: IRequestHandler<SendVideoOnYouTubeCommand>
{
	public async Task Handle(SendVideoOnYouTubeCommand request, CancellationToken ct)
	{
		var fileDtos = await storage.GetVideoFileMessageAsync(request.MessageId, provider.Current.UserId, ct);
		if (fileDtos is [])
			throw new MessageNotFoundException(request.MessageId);

		var accessToken = await storage.GetAccessTokenAsync(request.MessageId, provider.Current.UserId, ct);
		var telegram = await tokenService.GetTokenByMessageIdAsync(request.MessageId, ct);
		var bot = new TelegramBotClient(telegram.token, cancellationToken: ct);
		
		foreach (var fileDto in fileDtos)
		{
			using var stream = new MemoryStream();
			var file = await bot.GetInfoAndDownloadFile(fileDto.TgFileId, stream, ct);

			await UploadShortsAsync(accessToken, stream, "Funny", "Funny");
		}
	}

	public async Task<string> UploadShortsAsync(string token, MemoryStream stream, string title, string description)
	{
		var credential = GoogleCredential.FromAccessToken(token);
		// Если токен истек, нужно использовать RefreshToken (в простой реализации опускаем)

		var youtubeService = new YouTubeService(new BaseClientService.Initializer()
		{
			HttpClientInitializer = credential,
			ApplicationName = "MyShortsUploader"
		});

		var video = new Video
		{
			Snippet = new VideoSnippet
			{
				Title = title,
				Description = description + " #shorts",
				Tags = ["shorts", "vertical"],
				CategoryId = "22"
			},
			Status = new VideoStatus { PrivacyStatus = "public" } // video.Status, а не video.Snippet
		};
		string videoId = "";

		var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", stream, "video/*");

		videosInsertRequest.ResponseReceived += (v) =>
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
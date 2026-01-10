using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Shared;

/// <summary>
/// Сервис для работы с YouTube API
/// </summary>
public sealed class YouTubeService
{
	private const string ApplicationName = "TgPoster";
	private const string CategoryId = "22"; // Category ID для Shorts

	/// <summary>
	/// Загружает видео на YouTube
	/// </summary>
	/// <param name="account">Данные YouTube аккаунта</param>
	/// <param name="stream">Поток видео</param>
	/// <param name="title">Название видео</param>
	/// <param name="description">Описание видео</param>
	/// <param name="tags">Теги (через запятую)</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>ID загруженного видео</returns>
	public async Task<string> UploadVideoAsync(
		YouTubeAccountDto account,
		Stream stream,
		string? title = null,
		string? description = null,
		string? tags = null,
		CancellationToken ct = default
	)
	{
		title ??= account.DefaultTitle ?? "Видео";
		description ??= account.DefaultDescription ;
		ArgumentNullException.ThrowIfNull(account);
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentException.ThrowIfNullOrWhiteSpace(title);

		var youtubeService = CreateYouTubeService(account);
		var tagArray = ParseTags(tags ?? account.DefaultTags);

		var video = new Video
		{
			Snippet = new VideoSnippet
			{
				Title = title,
				Description = description,
				Tags = tagArray,
				CategoryId = CategoryId
			},
			Status = new VideoStatus { PrivacyStatus = "public" }
		};

		var videoId = "";
		var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", stream, "video/*");

		videosInsertRequest.ResponseReceived += v =>
		{
			videoId = v.Id;
		};

		var result = await videosInsertRequest.UploadAsync(ct);
		if (result.Status == UploadStatus.Failed)
		{
			throw result.Exception;
		}

		return videoId;
	}

	/// <summary>
	/// Создает сервис YouTube с авторизацией
	/// </summary>
	private Google.Apis.YouTube.v3.YouTubeService CreateYouTubeService(YouTubeAccountDto account)
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

		return new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = ApplicationName
		});
	}

	/// <summary>
	/// Парсит строку с тегами в массив
	/// </summary>
	private string[] ParseTags(string? tags)
	{
		return tags?
			       .Split(',')
			       .Select(t => t.Trim())
			       .Where(t => !string.IsNullOrWhiteSpace(t))
			       .ToArray()
		       ?? [];
	}
}
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3.Data;

namespace Shared.YouTube;

/// <summary>
///     Сервис для работы с YouTube API
/// </summary>
public sealed class YouTubeService
{
	private const string ApplicationName = "TgPoster";
	private const string CategoryId = "22"; // Category ID для Shorts

	/// <summary>
	///     Загружает видео на YouTube
	/// </summary>
	/// <param name="account">Данные YouTube аккаунта</param>
	/// <param name="stream">Поток видео</param>
	/// <param name="title">Название видео</param>
	/// <param name="description">Описание видео</param>
	/// <param name="tags">Теги (через запятую)</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Результат загрузки с ID видео и обновленными токенами</returns>
	public async Task<YouTubeUploadResult> UploadVideoAsync(
		YouTubeAccountDto account,
		Stream stream,
		string? title = null,
		string? description = null,
		string? tags = null,
		CancellationToken ct = default
	)
	{
		title ??= account.DefaultTitle ?? "Видео";
		description ??= account.DefaultDescription;
		ArgumentNullException.ThrowIfNull(account);
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentException.ThrowIfNullOrWhiteSpace(title);

		var (youtubeService, credential) = CreateYouTubeService(account);
		var tagArray = ParseTags(tags ?? account.DefaultTags ?? "shorts,vertical");

		var video = new Google.Apis.YouTube.v3.Data.Video
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

		var token = await credential.GetAccessTokenForRequestAsync(cancellationToken: ct);

		return new YouTubeUploadResult
		{
			VideoId = videoId,
			AccessToken = token,
			RefreshToken = credential.Token.RefreshToken
		};
	}

	/// <summary>
	///     Создает сервис YouTube с авторизацией
	/// </summary>
	private (Google.Apis.YouTube.v3.YouTubeService service, UserCredential credential) CreateYouTubeService(
		YouTubeAccountDto account
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

		var service = new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer
		{
			HttpClientInitializer = credential,
			ApplicationName = ApplicationName
		});

		return (service, credential);
	}

	/// <summary>
	///     Парсит строку с тегами в массив
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

/// <summary>
///     Результат загрузки видео на YouTube
/// </summary>
public sealed class YouTubeUploadResult
{
	/// <summary>
	///     ID загруженного видео на YouTube
	/// </summary>
	public required string VideoId { get; init; }

	/// <summary>
	///     Обновленный Access Token (может быть обновлен автоматически)
	/// </summary>
	public required string AccessToken { get; init; }

	/// <summary>
	///     Refresh Token (остается прежним или обновляется)
	/// </summary>
	public string? RefreshToken { get; init; }
}
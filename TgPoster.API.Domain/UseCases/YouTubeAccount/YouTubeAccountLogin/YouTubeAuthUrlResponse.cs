namespace TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;

/// <summary>
///     Ответ с URL для авторизации YouTube
/// </summary>
public sealed record YouTubeAuthUrlResponse
{
	public required string Url { get; init; }
}
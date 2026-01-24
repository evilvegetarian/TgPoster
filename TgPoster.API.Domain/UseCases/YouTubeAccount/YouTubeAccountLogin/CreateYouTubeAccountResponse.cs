namespace TgPoster.API.Domain.UseCases.YouTubeAccount.YouTubeAccountLogin;

/// <summary>
///     Ответ на создание Ютуб аккаунта
/// </summary>
public sealed record CreateYouTubeAccountResponse
{
	public required string Url { get; init; }
}
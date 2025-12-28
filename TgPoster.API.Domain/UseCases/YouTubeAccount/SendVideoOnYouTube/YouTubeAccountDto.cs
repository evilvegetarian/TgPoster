namespace TgPoster.API.Domain.UseCases.YouTubeAccount.SendVideoOnYouTube;

public class YouTubeAccountDto
{
	public required string AccessToken { get; set; }
	public string? RefreshToken { get; set; }
	public required string ClientId { get; set; }
	public required string ClientSecret { get; set; }
}

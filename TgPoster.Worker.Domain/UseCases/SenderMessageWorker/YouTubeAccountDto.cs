namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

public class YouTubeAccountDto
{
	public required string AccessToken { get; set; }
	public string? RefreshToken { get; set; }
	public required string ClientId { get; set; }
	public required string ClientSecret { get; set; }
	public string? DefaultTitle { get; set; }
	public string? DefaultDescription { get; set; }
	public string? DefaultTags { get; set; }
	public bool AutoPostingVideo { get; set; }
}

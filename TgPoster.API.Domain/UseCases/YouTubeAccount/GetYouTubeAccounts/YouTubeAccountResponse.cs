namespace TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;

public class YouTubeAccountResponse
{
	public required Guid Id { get; set; }
	public required string Name { get; set; }
	public string? DefaultTitle { get; set; }
	public string? DefaultDescription { get; set; }
	public string? DefaultTags { get; set; }
	public bool AutoPostingVideo { get; set; }
}
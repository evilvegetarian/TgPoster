namespace TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;

public sealed record YouTubeAccountResponse
{
	public required Guid Id { get; init; }
	public required string Name { get; init; }
	public string? DefaultTitle { get; init; }
	public string? DefaultDescription { get; init; }
	public string? DefaultTags { get; init; }
	public bool AutoPostingVideo { get; init; }
}
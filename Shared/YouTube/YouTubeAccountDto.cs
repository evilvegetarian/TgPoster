namespace Shared.YouTube;

/// <summary>
///     DTO для работы с YouTube аккаунтом
/// </summary>
public class YouTubeAccountDto
{
	public Guid Id { get; set; }
	public required string AccessToken { get; set; }
	public string? RefreshToken { get; set; }
	public required string ClientId { get; set; }
	public required string ClientSecret { get; set; }
	public string? DefaultTitle { get; set; }
	public string? DefaultDescription { get; set; }
	public string? DefaultTags { get; set; }
	public bool AutoPostingVideo { get; set; }
}
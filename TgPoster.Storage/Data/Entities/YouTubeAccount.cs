namespace TgPoster.Storage.Data.Entities;

public class YouTubeAccount : BaseEntity
{
	public required string Name { get; set; }
	public required string AccessToken { get; set; }
	public string? RefreshToken { get; set; }
	public required string ClientId { get; set; }
	public required string ClientSecret { get; set; }
	public string? DefaultTitle { get; set; }
	public string? DefaultDescription { get; set; }
	public string? DefaultTags { get; set; }
	public required Guid UserId { get; set; }

	#region Навигация

	public ICollection<Schedule> Schedules { get; set; } = [];
	public User User { get; set; } = null!;

	#endregion
}

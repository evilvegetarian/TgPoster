namespace TgPoster.Storage.Data.Entities;

public sealed class RefreshSession
{
    public required Guid RefreshToken { get; set; }
    public required Guid UserId { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public User User { get; set; }
}
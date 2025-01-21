namespace TgPoster.Storage.Entities;

public sealed class Session
{
    public Guid SessionId { get; set; }
    public required Guid UserId { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public User User { get; set; }
}
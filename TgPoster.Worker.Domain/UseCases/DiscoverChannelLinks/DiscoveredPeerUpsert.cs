namespace TgPoster.Worker.Domain.UseCases.DiscoverChannelLinks;

public sealed record DiscoveredPeerUpsert
{
	public string? Username { get; init; }
	public string? TgUrl { get; init; }
	public int? LastParsedId { get; init; }
	public long? TelegramId { get; init; }
	public string? PeerType { get; init; }
	public string? Title { get; init; }
	public int? ParticipantsCount { get; init; }
	public string? InviteHash { get; init; }
	public Guid? DiscoveredFromChannelId { get; init; }
	public bool MarkAsCompleted { get; init; }
}

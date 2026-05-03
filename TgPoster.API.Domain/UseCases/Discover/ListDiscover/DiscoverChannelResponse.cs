namespace TgPoster.API.Domain.UseCases.Discover.ListDiscover;

public sealed record DiscoverChannelResponse
{
    public Guid Id { get; init; }
    public string? Username { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? AvatarUrl { get; init; }
    public int? ParticipantsCount { get; init; }
    public string? PeerType { get; init; }
    public string? TgUrl { get; init; }
    public string? Category { get; init; }
    public string? Subcategory { get; init; }
    public string? Language { get; init; }
    public DateTimeOffset? LastDiscoveredAt { get; init; }
}

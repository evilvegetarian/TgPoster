namespace TgPoster.API.Domain.UseCases.Discover.SimilarChannels;

public interface ISimilarDiscoverStorage
{
	Task<SimilarDiscoverSeed?> GetSeedAsync(Guid id, CancellationToken ct);
	Task<Guid?> GetSessionAsync(Guid userId, CancellationToken ct);
	Task<List<SimilarDiscoverChannel>> FindLocalAsync(SimilarDiscoverSeed seed, CancellationToken ct);
	Task<List<KnownDiscoverPeer>> FindKnownAsync(long[] telegramIds, string[] usernames, CancellationToken ct);
	Task<int> AddAsync(Guid sourceId, Guid userId, IReadOnlyList<SimilarDiscoverChannel> channels, CancellationToken ct);
}

public sealed record SimilarDiscoverSeed(
	Guid Id, long? TelegramId, string? Username, string? PeerType,
	string? Category, string? Subcategory, string? Language, string[]? Tags);

public sealed record KnownDiscoverPeer(Guid Id, long? TelegramId, string? Username, bool Hidden);

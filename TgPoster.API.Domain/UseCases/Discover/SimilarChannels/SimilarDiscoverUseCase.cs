using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Security.IdentityServices;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;
using TgPoster.Telegram.Abstractions;

namespace TgPoster.API.Domain.UseCases.Discover.SimilarChannels;

internal sealed class SimilarDiscoverUseCase(
	ISimilarDiscoverStorage storage, IIdentityProvider identity, ITelegramMessageService telegram, IMemoryCache cache)
	: IRequestHandler<GetSimilarDiscoverQuery, SimilarDiscoverResponse>, IRequestHandler<SaveSimilarDiscoverCommand, int>
{
	public async Task<SimilarDiscoverResponse> Handle(GetSimilarDiscoverQuery request, CancellationToken ct)
	{
		var seed = await storage.GetSeedAsync(request.ChannelId, ct) ?? throw new DiscoveredChannelNotFoundException();
		if (seed.PeerType != "chat" && !string.IsNullOrEmpty(seed.Username)
			&& await storage.GetSessionAsync(identity.Current.UserId, ct) is { } sessionId)
		{
			var recommendations = await GetTelegramChannelsAsync(seed, sessionId, ct);
			if (recommendations.Count > 0)
			{
				var known = await storage.FindKnownAsync(
					recommendations.Select(c => c.TelegramId!.Value).ToArray(),
					recommendations.Select(c => c.Username!).ToArray(), ct);
				var channels = new List<SimilarDiscoverChannel>();
				foreach (var candidate in recommendations)
				{
					var matches = known.Where(p => p.TelegramId == candidate.TelegramId ||
						string.Equals(p.Username, candidate.Username, StringComparison.OrdinalIgnoreCase)).ToList();
					if (matches.Any(p => p.Hidden || p.Id == seed.Id ||
						p.TelegramId.HasValue && p.TelegramId != candidate.TelegramId))
						continue;

					channels.Add(candidate with { DiscoverId = matches.FirstOrDefault()?.Id });
				}
				if (channels.Count > 0)
					return new SimilarDiscoverResponse(SimilarDiscoverSource.Telegram, channels);
			}
		}

		return new SimilarDiscoverResponse(SimilarDiscoverSource.Catalog, await storage.FindLocalAsync(seed, ct));
	}

	public async Task<int> Handle(SaveSimilarDiscoverCommand request, CancellationToken ct)
	{
		var selected = request.TelegramIds.ToHashSet();
		if (selected.Count == 0)
			throw new InvalidDiscoverSelectionException();

		var preview = await Handle(new GetSimilarDiscoverQuery(request.ChannelId), ct);
		var matches = preview.Channels.Where(c => c.TelegramId is { } id && selected.Contains(id)).ToList();
		if (preview.Source != SimilarDiscoverSource.Telegram || matches.Count != selected.Count)
			throw new InvalidDiscoverSelectionException();

		var newChannels = matches.Where(c => c.DiscoverId is null).ToList();
		return newChannels.Count == 0 ? 0 : await storage.AddAsync(request.ChannelId, identity.Current.UserId, newChannels, ct);
	}

	private async Task<IReadOnlyList<SimilarDiscoverChannel>> GetTelegramChannelsAsync(
		SimilarDiscoverSeed seed, Guid sessionId, CancellationToken ct)
	{
		var key = (nameof(SimilarDiscoverUseCase), identity.Current.UserId, sessionId, seed.Id, seed.TelegramId, seed.Username);
		if (cache.TryGetValue(key, out List<SimilarDiscoverChannel>? cached) && cached is not null)
			return cached;

		var resolved = await telegram.ResolveChannelAsync(sessionId, seed.Username!, ct, false);
		if (!resolved.IsSuccess || resolved.Value is not { IsChannel: true, IsGroup: false } channel ||
			(seed.TelegramId.HasValue && seed.TelegramId != channel.Id))
			return [];

		var result = await telegram.GetChannelRecommendationsAsync(sessionId, channel.Peer, ct);
		if (!result.IsSuccess || result.Value is null)
			return [];

		var candidates = result.Value
			.Where(c => c.Id != channel.Id && !string.IsNullOrEmpty(c.Username) &&
				!string.Equals(c.Username, seed.Username, StringComparison.OrdinalIgnoreCase))
			.DistinctBy(c => c.Id)
			.DistinctBy(c => c.Username, StringComparer.OrdinalIgnoreCase)
			.Select(c => new SimilarDiscoverChannel(null, c.Id, c.Username, c.Title, $"https://t.me/{c.Username}", c.ParticipantsCount))
			.ToList();
		cache.Set(key, candidates, TimeSpan.FromMinutes(5));
		return candidates;
	}
}

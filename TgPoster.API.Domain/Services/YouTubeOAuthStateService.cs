using Microsoft.Extensions.Caching.Memory;

namespace TgPoster.API.Domain.Services;

public sealed class YouTubeOAuthStateService(IMemoryCache cache, TimeProvider timeProvider)
{
	private readonly object gate = new();

	public string Create(Guid accountId, Guid userId)
	{
		var state = Guid.NewGuid().ToString("N");
		cache.Set($"youtube-oauth:{state}",
			new StateContext(accountId, userId, timeProvider.GetUtcNow().AddMinutes(10)), TimeSpan.FromMinutes(10));
		return state;
	}

	public StateContext? Take(string state)
	{
		if (!Guid.TryParseExact(state, "N", out _))
			return null;
		lock (gate)
		{
			var key = $"youtube-oauth:{state}";
			if (!cache.TryGetValue<StateContext>(key, out var context))
				return null;
			cache.Remove(key);
			return context is not null && context.ExpiresAt > timeProvider.GetUtcNow() ? context : null;
		}
	}

	public sealed record StateContext(Guid AccountId, Guid UserId, DateTimeOffset ExpiresAt);
}

using Microsoft.Extensions.Caching.Memory;
using Shared.Social.Bluesky;

namespace TgPoster.Worker.Domain.UseCases.CrossPosting.Publishing.Bluesky;

/// <summary>
///     Кеш сессий Bluesky в памяти воркера
/// </summary>
internal sealed class BlueskySessionCache(IMemoryCache cache)
{
	private const string CacheKeyPrefix = "bluesky-session:";
	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(90);

	/// <summary>
	///     Получить сессию из кеша или создать новую
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="identifier"></param>
	/// <param name="appPassword"></param>
	/// <param name="client"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public Task<BlueskyResult<BlueskySession>> GetAsync(
		Guid accountId,
		string identifier,
		string appPassword,
		IBlueskyClient client,
		CancellationToken ct)
	{
		var key = CacheKeyPrefix + accountId;

		if (cache.TryGetValue<BlueskySession>(key, out var cached))
		{
			return Task.FromResult(BlueskyResult<BlueskySession>.Ok(cached!));
		}

		return CreateAndCacheAsync(key, identifier, appPassword, client, ct);
	}

	/// <summary>
	///     Сбросить кеш сессии для аккаунта
	/// </summary>
	/// <param name="accountId"></param>
	public void Invalidate(Guid accountId) => cache.Remove(CacheKeyPrefix + accountId);

	private async Task<BlueskyResult<BlueskySession>> CreateAndCacheAsync(
		string key,
		string identifier,
		string appPassword,
		IBlueskyClient client,
		CancellationToken ct)
	{
		var result = await client.CreateSessionAsync(identifier, appPassword, ct);

		if (result.IsSuccess && result.Value is not null)
		{
			cache.Set(key, result.Value, CacheDuration);
		}

		return result;
	}
}

using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.Tests.YouTubeAccount;

public sealed class YouTubeOAuthStateServiceShould
{
	[Fact]
	public async Task GrantStateToOnlyOneConcurrentCallback()
	{
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var sut = new YouTubeOAuthStateService(cache, TimeProvider.System);
		var accountId = Guid.NewGuid();
		var userId = Guid.NewGuid();
		var state = sut.Create(accountId, userId);
		var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		var callbacks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
		{
			await start.Task;
			return sut.Take(state);
		})).ToArray();
		start.SetResult();
		var results = await Task.WhenAll(callbacks);

		var granted = results.Where(x => x is not null).ToList();
		granted.Count.ShouldBe(1);
		granted[0]!.AccountId.ShouldBe(accountId);
		granted[0]!.UserId.ShouldBe(userId);
	}
}

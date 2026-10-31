using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Storages;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public sealed class CallBackYouTubeStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	[Fact]
	public async Task GetClients_ShouldReturnCredentialsOnlyToOwner()
	{
		await using var context = fixture.GetDbContext();
		var account = new YouTubeAccountBuilder(context).Create();
		var storage = new CallBackYouTubeStorage(context);

		var own = await storage.GetClients(account.Id, account.UserId, CancellationToken.None);
		var foreign = await storage.GetClients(account.Id, Guid.NewGuid(), CancellationToken.None);

		own.ClientId.ShouldBe(account.ClientId);
		own.ClientSecret.ShouldBe(account.ClientSecret);
		foreign.ClientId.ShouldBeNull();
		foreign.ClientSecret.ShouldBeNull();
	}

	[Fact]
	public async Task AddToken_ShouldNotChangeForeignAccount()
	{
		await using var context = fixture.GetDbContext();
		var account = new YouTubeAccountBuilder(context).Create();
		var oldToken = account.AccessToken;
		var storage = new CallBackYouTubeStorage(context);

		await Should.ThrowAsync<InvalidOperationException>(() => storage.AddToken(account.Id, Guid.NewGuid(),
			"new-test-token", "new-test-refresh", "Channel", null, CancellationToken.None));

		await using var check = fixture.GetDbContext();
		(await check.YouTubeAccounts.FirstAsync(x => x.Id == account.Id)).AccessToken.ShouldBe(oldToken);
	}
}

using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Storages.SocialAccounts;
using TgPoster.Storage.Tests.Builders;

namespace TgPoster.Storage.Tests.Tests;

public class SocialAccountStorageShould(StorageTestFixture fixture) : IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly SocialAccountStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task UpsertAsync_WithNewAccount_ShouldCreateAccount()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var name = "test.bsky.social";
		var did = "did:plc:abc";

		var id = await sut.UpsertAsync(user.Id, SocialPlatform.Bluesky, name, did, "secret", null, CancellationToken.None);

		var account = await context.SocialAccounts
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id);

		account.ShouldNotBeNull();
		account.UserId.ShouldBe(user.Id);
		account.Platform.ShouldBe(SocialPlatform.Bluesky);
		account.Name.ShouldBe(name);
		account.ExternalUserId.ShouldBe(did);
		account.Secret.ShouldBe("secret");
		account.Status.ShouldBe(SocialAccountStatus.Active);
	}

	[Fact]
	public async Task UpsertAsync_WithExistingDid_ShouldUpdateSecretAndNotCreateDuplicate()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var name = "test.bsky.social";
		var did = "did:plc:abc";

		var firstId = await sut.UpsertAsync(user.Id, SocialPlatform.Bluesky, name, did, "secret1", null, CancellationToken.None);
		var secondId = await sut.UpsertAsync(user.Id, SocialPlatform.Bluesky, name, did, "secret2", null, CancellationToken.None);

		firstId.ShouldBe(secondId);

		var accounts = await context.SocialAccounts
			.AsNoTracking()
			.Where(x => x.UserId == user.Id && x.ExternalUserId == did)
			.ToListAsync();

		accounts.Count.ShouldBe(1);
		accounts[0].Secret.ShouldBe("secret2");
		accounts[0].Status.ShouldBe(SocialAccountStatus.Active);
		accounts[0].LastError.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithLinkedTargets_ShouldSoftDeleteAccountAndTargets()
	{
		var user = await new UserBuilder(context).CreateAsync();
		var account = new SocialAccountBuilder(context).WithUserId(user.Id).Create();
		var target = new CrossPostTargetBuilder(context).WithSocialAccount(account).Create();

		await sut.DeleteAsync(account.Id, CancellationToken.None);

		var deletedAccount = await context.SocialAccounts
			.AsNoTracking()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => x.Id == account.Id);

		var deletedTarget = await context.CrossPostTargets
			.AsNoTracking()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(x => x.Id == target.Id);

		deletedAccount.ShouldNotBeNull();
		deletedAccount.Deleted.ShouldNotBeNull();
		deletedTarget.ShouldNotBeNull();
		deletedTarget.Deleted.ShouldNotBeNull();
	}
}

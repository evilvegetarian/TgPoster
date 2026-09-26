using Shared.Enums;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.API.Tests.Helper;

internal class SocialAccountBuilder(PosterContext context)
{
	private readonly SocialAccount account = new()
	{
		Id = Guid.NewGuid(),
		UserId = new UserBuilder(context).Create().Id,
		Platform = SocialPlatform.Bluesky,
		Name = "test.bsky.social",
		ExternalUserId = "did:plc:" + Guid.NewGuid().ToString("N"),
		Secret = "encrypted",
		Status = SocialAccountStatus.Active
	};

	public SocialAccountBuilder WithUserId(Guid userId)
	{
		account.UserId = userId;
		return this;
	}

	public SocialAccountBuilder WithName(string name)
	{
		account.Name = name;
		return this;
	}

	public SocialAccount Create()
	{
		context.SocialAccounts.AddRange(account);
		context.SaveChanges();
		return account;
	}
}

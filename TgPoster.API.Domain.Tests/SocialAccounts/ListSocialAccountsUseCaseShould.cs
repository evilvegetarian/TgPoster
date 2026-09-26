using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.SocialAccounts.ListSocialAccounts;

namespace TgPoster.API.Domain.Tests.SocialAccounts;

public class ListSocialAccountsUseCaseShould
{
	private readonly Mock<IListSocialAccountsStorage> storage;
	private readonly ListSocialAccountsUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public ListSocialAccountsUseCaseShould()
	{
		storage = new Mock<IListSocialAccountsStorage>();
		var identityProvider = new Mock<IIdentityProvider>();
		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new ListSocialAccountsUseCase(storage.Object, identityProvider.Object);
	}

	[Fact]
	public async Task ReturnAccountsFromStorage_ForCurrentUser()
	{
		var expected = new List<SocialAccountResponse>
		{
			new()
			{
				Id = Guid.NewGuid(),
				Platform = SocialPlatform.Bluesky,
				Name = "test.bsky.social",
				Status = SocialAccountStatus.Active,
				Created = DateTimeOffset.UtcNow
			}
		};
		storage.Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

		var result = await sut.Handle(new ListSocialAccountsQuery(), CancellationToken.None);

		result.ShouldBe(expected);
		storage.Verify(x => x.GetAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
	}
}

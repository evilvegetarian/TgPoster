using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.YouTubeAccount.GetYouTubeAccounts;

namespace TgPoster.API.Domain.Tests.YouTubeAccount;

public sealed class GetYouTubeAccountsUseCaseShould
{
	[Fact]
	public async Task RequestOnlyAccountsOfCurrentUser()
	{
		var userId = Guid.NewGuid();
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		var storage = new Mock<IGetYouTubeAccountsStorage>();
		storage.Setup(x => x.GetYouTubeAccounts(userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
		var sut = new GetYouTubeAccountsUseCase(storage.Object, identity.Object);

		var response = await sut.Handle(new GetYouTubeAccountsQuery(), CancellationToken.None);

		response.Items.ShouldBeEmpty();
		storage.Verify(x => x.GetYouTubeAccounts(userId, It.IsAny<CancellationToken>()), Times.Once);
	}
}

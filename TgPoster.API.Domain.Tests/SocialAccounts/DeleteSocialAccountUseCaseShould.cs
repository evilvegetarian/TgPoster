using Moq;
using Moq.Language.Flow;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.SocialAccounts.DeleteSocialAccount;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.SocialAccounts;

public class DeleteSocialAccountUseCaseShould
{
	private readonly ISetup<IDeleteSocialAccountStorage, Task<bool>> existsSetup;
	private readonly Mock<IDeleteSocialAccountStorage> storage;
	private readonly DeleteSocialAccountUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public DeleteSocialAccountUseCaseShould()
	{
		storage = new Mock<IDeleteSocialAccountStorage>();
		var identityProvider = new Mock<IIdentityProvider>();
		identityProvider.Setup(x => x.Current).Returns(new Identity(userId));

		existsSetup = storage.Setup(s => s.ExistsAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()));
		sut = new DeleteSocialAccountUseCase(storage.Object, identityProvider.Object);
	}

	[Fact]
	public async Task DeleteAccount_WhenExistsAndBelongsToUser()
	{
		var accountId = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		await sut.Handle(new DeleteSocialAccountCommand(accountId), CancellationToken.None);

		storage.Verify(s => s.DeleteAsync(accountId, CancellationToken.None), Times.Once);
	}

	[Fact]
	public async Task ThrowSocialAccountNotFoundException_WhenNotExists()
	{
		var accountId = Guid.NewGuid();
		existsSetup.ReturnsAsync(false);

		await Should.ThrowAsync<SocialAccountNotFoundException>(async () =>
			await sut.Handle(new DeleteSocialAccountCommand(accountId), CancellationToken.None));

		storage.Verify(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task CheckOwnership_BeforeDeleting()
	{
		var accountId = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		await sut.Handle(new DeleteSocialAccountCommand(accountId), CancellationToken.None);

		storage.Verify(s => s.ExistsAsync(accountId, userId, CancellationToken.None), Times.Once);
	}
}

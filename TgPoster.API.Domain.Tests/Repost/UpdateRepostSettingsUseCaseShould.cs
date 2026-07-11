using Moq;
using Moq.Language.Flow;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostSettings;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Repost;

public class UpdateRepostSettingsUseCaseShould
{
	private readonly ISetup<IUpdateRepostSettingsStorage, Task<bool>> existsSetup;
	private readonly Mock<IUpdateRepostSettingsStorage> storage;
	private readonly UpdateRepostSettingsUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public UpdateRepostSettingsUseCaseShould()
	{
		storage = new Mock<IUpdateRepostSettingsStorage>();
		var identityProvider = new Mock<IIdentityProvider>();

		var identity = new Identity(userId);
		identityProvider.Setup(x => x.Current).Returns(identity);

		existsSetup = storage.Setup(s =>
			s.SettingsExistsAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()));

		sut = new UpdateRepostSettingsUseCase(storage.Object, identityProvider.Object);
	}

	[Fact]
	public async Task UpdateSettings_WhenExistsAndBelongsToUser()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		await sut.Handle(
			new UpdateRepostSettingsCommand(id, false, 10, 60, 2, 30, 5),
			CancellationToken.None);

		storage.Verify(s => s.UpdateSettingsAsync(id, false, 10, 60, 2, 30, 5, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenNotExists()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(false);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(async () =>
			await sut.Handle(
				new UpdateRepostSettingsCommand(id, true, 0, 0, 1, 0, null),
				CancellationToken.None));

		storage.Verify(s => s.UpdateSettingsAsync(
				It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
				It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task CheckOwnershipWithCurrentUserId()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		await sut.Handle(
			new UpdateRepostSettingsCommand(id, true, 0, 0, 1, 0, null),
			CancellationToken.None);

		storage.Verify(s => s.SettingsExistsAsync(id, userId, It.IsAny<CancellationToken>()), Times.Once);
	}
}

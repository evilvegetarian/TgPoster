using Moq;
using Moq.Language.Flow;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostSettings;

namespace TgPoster.API.Domain.Tests.Repost;

public class UpdateRepostSettingsUseCaseShould
{
	private readonly Mock<IUpdateRepostSettingsStorage> storage;
	private readonly ISetup<IUpdateRepostSettingsStorage, Task<bool>> existsSetup;
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

		await sut.Handle(new UpdateRepostSettingsCommand(id, false), CancellationToken.None);

		storage.Verify(s => s.UpdateSettingsAsync(id, false, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenNotExists()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(false);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(
			async () => await sut.Handle(new UpdateRepostSettingsCommand(id, true), CancellationToken.None));

		storage.Verify(s => s.UpdateSettingsAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task CheckOwnershipWithCurrentUserId()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		await sut.Handle(new UpdateRepostSettingsCommand(id, true), CancellationToken.None);

		storage.Verify(s => s.SettingsExistsAsync(id, userId, It.IsAny<CancellationToken>()), Times.Once);
	}
}

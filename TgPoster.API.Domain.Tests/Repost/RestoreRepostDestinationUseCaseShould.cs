using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.RestoreRepostDestination;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Repost;

public sealed class RestoreRepostDestinationUseCaseShould
{
	private const long ChatId = 123456;

	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Guid destinationId = Guid.NewGuid();
	private readonly Mock<IRestoreRepostDestinationStorage> storage = new();
	private readonly RestoreRepostDestinationUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public RestoreRepostDestinationUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.SettingsExistsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(s => s.GetRemovedAsync(settingsId, destinationId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new RemovedRepostDestinationDto(ChatId, "Канал"));

		sut = new RestoreRepostDestinationUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task Restore_WhenChatIsNotInListAgain()
	{
		await sut.Handle(new RestoreRepostDestinationCommand(settingsId, destinationId), CancellationToken.None);

		storage.Verify(s => s.RestoreAsync(destinationId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenSettingsNotOwnedByUser()
	{
		storage.Setup(s => s.SettingsExistsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(() =>
			sut.Handle(new RestoreRepostDestinationCommand(settingsId, destinationId), CancellationToken.None));

		VerifyNotRestored();
	}

	[Fact]
	public async Task ThrowRepostDestinationNotFoundException_WhenDestinationIsNotRemoved()
	{
		storage.Setup(s => s.GetRemovedAsync(settingsId, destinationId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((RemovedRepostDestinationDto?)null);

		await Should.ThrowAsync<RepostDestinationNotFoundException>(() =>
			sut.Handle(new RestoreRepostDestinationCommand(settingsId, destinationId), CancellationToken.None));

		VerifyNotRestored();
	}

	[Fact]
	public async Task ThrowAlreadyExists_WhenSameChatAddedAgain()
	{
		storage.Setup(s => s.ChatInListAsync(settingsId, ChatId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

		await Should.ThrowAsync<RepostDestinationAlreadyExistsException>(() =>
			sut.Handle(new RestoreRepostDestinationCommand(settingsId, destinationId), CancellationToken.None));

		VerifyNotRestored();
	}

	private void VerifyNotRestored() =>
		storage.Verify(s => s.RestoreAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
}

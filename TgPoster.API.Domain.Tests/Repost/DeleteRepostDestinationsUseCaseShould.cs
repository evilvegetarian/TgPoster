using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestinations;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Repost;

public sealed class DeleteRepostDestinationsUseCaseShould
{
	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Mock<IDeleteRepostDestinationsStorage> storage = new();
	private readonly DeleteRepostDestinationsUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public DeleteRepostDestinationsUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.SettingsExistsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		sut = new DeleteRepostDestinationsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task DeleteDistinctDestinations_WhenAllBelongToSettings()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		SetupExisting(first, second);

		await sut.Handle(new DeleteRepostDestinationsCommand(settingsId, [first, second, first]), CancellationToken.None);

		storage.Verify(s => s.DeleteAsync(
			It.Is<List<Guid>>(ids => ids.Count == 2 && ids.Contains(first) && ids.Contains(second)),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenSettingsNotOwnedByUser()
	{
		storage.Setup(s => s.SettingsExistsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(async () =>
			await sut.Handle(new DeleteRepostDestinationsCommand(settingsId, [Guid.NewGuid()]), CancellationToken.None));

		VerifyNotDeleted();
	}

	[Fact]
	public async Task ThrowAndDeleteNothing_WhenAnyDestinationIsMissing()
	{
		var existing = Guid.NewGuid();
		var missing = Guid.NewGuid();
		SetupExisting(existing);

		var exception = await Should.ThrowAsync<RepostDestinationNotFoundException>(async () =>
			await sut.Handle(new DeleteRepostDestinationsCommand(settingsId, [existing, missing]), CancellationToken.None));

		exception.Message.ShouldContain(missing.ToString());
		VerifyNotDeleted();
	}

	private void SetupExisting(params Guid[] ids) =>
		storage.Setup(s => s.GetExistingIdsAsync(settingsId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ids.ToList());

	private void VerifyNotDeleted() =>
		storage.Verify(s => s.DeleteAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
}

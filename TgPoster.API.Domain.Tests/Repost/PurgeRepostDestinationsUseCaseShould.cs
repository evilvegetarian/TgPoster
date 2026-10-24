using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.PurgeRepostDestinations;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Repost;

public sealed class PurgeRepostDestinationsUseCaseShould
{
	private readonly Guid settingsId = Guid.NewGuid();
	private readonly Mock<IPurgeRepostDestinationsStorage> storage = new();
	private readonly PurgeRepostDestinationsUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public PurgeRepostDestinationsUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(s => s.SettingsExistsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		sut = new PurgeRepostDestinationsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task PurgeDistinctDestinations_WhenAllBelongToSettings()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		SetupPurgeable(first, second);

		await sut.Handle(new PurgeRepostDestinationsCommand(settingsId, [first, second, first]), CancellationToken.None);

		storage.Verify(s => s.PurgeAsync(
			It.Is<List<Guid>>(ids => ids.Count == 2 && ids.Contains(first) && ids.Contains(second)),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowRepostSettingsNotFoundException_WhenSettingsNotOwnedByUser()
	{
		storage.Setup(s => s.SettingsExistsAsync(settingsId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<RepostSettingsNotFoundException>(() =>
			sut.Handle(new PurgeRepostDestinationsCommand(settingsId, [Guid.NewGuid()]), CancellationToken.None));

		VerifyNotPurged();
	}

	[Fact]
	public async Task ThrowAndPurgeNothing_WhenAnyDestinationIsMissingOrAlreadyPurged()
	{
		var purgeable = Guid.NewGuid();
		var missing = Guid.NewGuid();
		SetupPurgeable(purgeable);

		var exception = await Should.ThrowAsync<RepostDestinationNotFoundException>(() =>
			sut.Handle(new PurgeRepostDestinationsCommand(settingsId, [purgeable, missing]), CancellationToken.None));

		exception.Message.ShouldContain(missing.ToString());
		VerifyNotPurged();
	}

	private void SetupPurgeable(params Guid[] ids) =>
		storage.Setup(s => s.GetPurgeableIdsAsync(settingsId, It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ids.ToList());

	private void VerifyNotPurged() =>
		storage.Verify(s => s.PurgeAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
}

using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.UpdateDiscoverSettings;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Discover;

public class UpdateDiscoverSettingsUseCaseShould
{
	private readonly Guid userId = Guid.NewGuid();
	private readonly Guid ownSessionId = Guid.NewGuid();
	private readonly Guid otherOwnSessionId = Guid.NewGuid();
	private readonly Mock<IUpdateDiscoverSettingsStorage> storage;
	private readonly UpdateDiscoverSettingsUseCase sut;

	public UpdateDiscoverSettingsUseCaseShould()
	{
		storage = new Mock<IUpdateDiscoverSettingsStorage>();
		storage.Setup(s => s.GetUserSessionIdsAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([ownSessionId, otherOwnSessionId]);
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new UpdateDiscoverSettingsUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task SaveSettings_ForCurrentUser()
	{
		var saved = CaptureSaved();

		await sut.Handle(ValidCommand() with { IsEnabled = false, IntervalMinutes = 30 }, CancellationToken.None);

		saved.Command.ShouldNotBeNull();
		saved.Command.IsEnabled.ShouldBeFalse();
		saved.Command.IntervalMinutes.ShouldBe(30);
		saved.Command.ChannelsPerSession.ShouldBe(2);
		saved.UserId.ShouldBe(userId);
	}

	[Fact]
	public async Task Throw_WhenSessionBelongsToAnotherUser()
	{
		var foreignSessionId = Guid.NewGuid();

		var exception = await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(
			() => sut.Handle(
				ValidCommand() with { TelegramSessionIds = [ownSessionId, foreignSessionId] },
				CancellationToken.None));

		exception.Message.ShouldContain(foreignSessionId.ToString());
		VerifyNotSaved();
	}

	[Fact]
	public async Task SaveSeveralOwnSessions_WithoutDuplicates()
	{
		var saved = CaptureSaved();

		await sut.Handle(
			ValidCommand() with { TelegramSessionIds = [ownSessionId, otherOwnSessionId, ownSessionId] },
			CancellationToken.None);

		saved.Command.ShouldNotBeNull();
		saved.Command.TelegramSessionIds.ShouldBe([ownSessionId, otherOwnSessionId]);
	}

	[Fact]
	public async Task SaveEmptySelection_WithoutCheckingOwnership()
	{
		var saved = CaptureSaved();

		await sut.Handle(ValidCommand(), CancellationToken.None);

		storage.Verify(s => s.GetUserSessionIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		saved.Command.ShouldNotBeNull();
		saved.Command.TelegramSessionIds.ShouldBeEmpty();
	}

	private SavedCall CaptureSaved()
	{
		var call = new SavedCall();
		storage.Setup(s => s.SaveDiscoverSettingsAsync(
				It.IsAny<UpdateDiscoverSettingsCommand>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.Callback<UpdateDiscoverSettingsCommand, Guid, CancellationToken>((command, user, _) =>
			{
				call.Command = command;
				call.UserId = user;
			});
		return call;
	}

	private void VerifyNotSaved() =>
		storage.Verify(s => s.SaveDiscoverSettingsAsync(
				It.IsAny<UpdateDiscoverSettingsCommand>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);

	private static UpdateDiscoverSettingsCommand ValidCommand() => new(true, 60, 2, []);

	private sealed class SavedCall
	{
		public UpdateDiscoverSettingsCommand? Command { get; set; }
		public Guid UserId { get; set; }
	}
}

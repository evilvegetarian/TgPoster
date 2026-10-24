using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Parse;

public sealed class UpdateParseChannelUseCaseShould
{
	private readonly Mock<IUpdateParseChannelStorage> storage = new();
	private readonly UpdateParseChannelUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public UpdateParseChannelUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		sut = new UpdateParseChannelUseCase(identity.Object, storage.Object);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PassSkipAdsToStorage_WhenUpdating(bool skipAds)
	{
		var command = CreateCommand(skipAds);
		SetupOwned(command);

		await sut.Handle(command, CancellationToken.None);

		storage.Verify(x => x.UpdateParseChannelAsync(
			It.Is<UpdateParseChannelCommand>(c => c.Id == command.Id && c.SkipAds == skipAds),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowNotFound_WhenSourceBelongsToAnotherUser()
	{
		var command = CreateCommand();
		SetupOwned(command);
		storage.Setup(x => x.ExistParseChannelAsync(command.Id, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<ParseChannelNotFoundException>(() => sut.Handle(command, CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowNotFound_WhenScheduleBelongsToAnotherUser()
	{
		var command = CreateCommand();
		SetupOwned(command);
		storage.Setup(x => x.ExistScheduleAsync(command.ScheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<ScheduleNotFoundException>(() => sut.Handle(command, CancellationToken.None));

		VerifyNotUpdated();
	}

	[Fact]
	public async Task ThrowNotFound_WhenTelegramSessionBelongsToAnotherUser()
	{
		var command = CreateCommand();
		SetupOwned(command);
		storage.Setup(x => x.ExistTelegramSessionAsync(command.TelegramSessionId, userId,
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<TelegramSessionEntityNotFoundException>(() =>
			sut.Handle(command, CancellationToken.None));

		VerifyNotUpdated();
	}

	private static UpdateParseChannelCommand CreateCommand(bool skipAds = true) =>
		new(Guid.NewGuid(), "source_channel", true, Guid.NewGuid(), false, false, [], false, null, null, false,
			Guid.NewGuid(), skipAds);

	private void SetupOwned(UpdateParseChannelCommand command)
	{
		storage.Setup(x => x.ExistParseChannelAsync(command.Id, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(x => x.ExistScheduleAsync(command.ScheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(x => x.ExistTelegramSessionAsync(command.TelegramSessionId, userId,
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
	}

	private void VerifyNotUpdated() =>
		storage.Verify(x => x.UpdateParseChannelAsync(It.IsAny<UpdateParseChannelCommand>(),
			It.IsAny<CancellationToken>()), Times.Never);
}

using Moq;
using Moq.Language.Flow;
using Shouldly;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

namespace TgPoster.API.Domain.Tests.Repost;

public class UpdateRepostDestinationUseCaseShould
{
	private readonly Mock<IUpdateRepostDestinationStorage> storage;
	private readonly ISetup<IUpdateRepostDestinationStorage, Task<bool>> existsSetup;
	private readonly UpdateRepostDestinationUseCase sut;

	public UpdateRepostDestinationUseCaseShould()
	{
		storage = new Mock<IUpdateRepostDestinationStorage>();
		existsSetup = storage.Setup(s =>
			s.DestinationExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()));

		sut = new UpdateRepostDestinationUseCase(storage.Object);
	}

	[Fact]
	public async Task UpdateDestination_WhenAllParametersAreValid()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		var command = new UpdateRepostDestinationCommand(id, true, 10, 60, 1, 50, 100);

		await sut.Handle(command, CancellationToken.None);

		storage.Verify(s => s.UpdateDestinationAsync(
			id, true, 10, 60, 1, 50, 100, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ThrowRepostDestinationNotFoundException_WhenNotExists()
	{
		existsSetup.ReturnsAsync(false);

		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, 0, 0, 1, 0, null);

		await Should.ThrowAsync<RepostDestinationNotFoundException>(
			async () => await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowException_WhenDelayMinSecondsIsNegative()
	{
		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, -1, 60, 1, 0, null);

		await Should.ThrowAsync<InvalidRepostSettingsException>(
			async () => await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowException_WhenDelayMaxLessThanDelayMin()
	{
		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, 60, 10, 1, 0, null);

		await Should.ThrowAsync<InvalidRepostSettingsException>(
			async () => await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowException_WhenRepostEveryNthIsZero()
	{
		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, 0, 60, 0, 0, null);

		await Should.ThrowAsync<InvalidRepostSettingsException>(
			async () => await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowException_WhenSkipProbabilityIsNegative()
	{
		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, 0, 60, 1, -1, null);

		await Should.ThrowAsync<InvalidRepostSettingsException>(
			async () => await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowException_WhenSkipProbabilityExceeds100()
	{
		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, 0, 60, 1, 101, null);

		await Should.ThrowAsync<InvalidRepostSettingsException>(
			async () => await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task ThrowException_WhenMaxRepostsPerDayIsZero()
	{
		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, 0, 60, 1, 0, 0);

		await Should.ThrowAsync<InvalidRepostSettingsException>(
			async () => await sut.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task AllowNullMaxRepostsPerDay()
	{
		var id = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		var command = new UpdateRepostDestinationCommand(id, true, 0, 0, 1, 0, null);

		await sut.Handle(command, CancellationToken.None);

		storage.Verify(s => s.UpdateDestinationAsync(
			id, true, 0, 0, 1, 0, null, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NotCallStorage_WhenValidationFails()
	{
		var command = new UpdateRepostDestinationCommand(Guid.NewGuid(), true, -1, 60, 1, 0, null);

		await Should.ThrowAsync<InvalidRepostSettingsException>(
			async () => await sut.Handle(command, CancellationToken.None));

		storage.Verify(s => s.DestinationExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
		storage.Verify(s => s.UpdateDestinationAsync(
			It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(),
			It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
	}
}

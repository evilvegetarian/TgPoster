using Moq;
using Moq.Language.Flow;
using Shouldly;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Repost;

public class UpdateRepostDestinationUseCaseShould
{
	private readonly ISetup<IUpdateRepostDestinationStorage, Task<bool>> existsSetup;
	private readonly Mock<IUpdateRepostDestinationStorage> storage;
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

		await Should.ThrowAsync<RepostDestinationNotFoundException>(async () =>
			await sut.Handle(command, CancellationToken.None));
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
}

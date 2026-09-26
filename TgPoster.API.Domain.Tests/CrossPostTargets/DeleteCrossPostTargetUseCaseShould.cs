using Moq;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.UseCases.CrossPostTargets.DeleteCrossPostTarget;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.CrossPostTargets;

public class DeleteCrossPostTargetUseCaseShould
{
	private readonly Guid id = Guid.NewGuid();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Mock<IDeleteCrossPostTargetStorage> storage = new();
	private readonly DeleteCrossPostTargetUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public DeleteCrossPostTargetUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(x => x.ScheduleExistsAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(x => x.ExistsAsync(id, scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		sut = new DeleteCrossPostTargetUseCase(storage.Object, identity.Object);
	}

	[Fact]
	public async Task ThrowScheduleNotFound_WhenScheduleNotOwned()
	{
		storage.Setup(x => x.ScheduleExistsAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<ScheduleNotFoundException>(async () =>
			await sut.Handle(new DeleteCrossPostTargetCommand(scheduleId, id), CancellationToken.None));
	}

	[Fact]
	public async Task ThrowTargetNotFound_WhenTargetMissing()
	{
		storage.Setup(x => x.ExistsAsync(id, scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<CrossPostTargetNotFoundException>(async () =>
			await sut.Handle(new DeleteCrossPostTargetCommand(scheduleId, id), CancellationToken.None));

		storage.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task DeleteTarget()
	{
		await sut.Handle(new DeleteCrossPostTargetCommand(scheduleId, id), CancellationToken.None);

		storage.Verify(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
	}
}

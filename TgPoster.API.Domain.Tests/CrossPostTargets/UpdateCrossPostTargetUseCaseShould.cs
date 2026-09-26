using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.CrossPostTargets.UpdateCrossPostTarget;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.CrossPostTargets;

public class UpdateCrossPostTargetUseCaseShould
{
	private readonly Guid id = Guid.NewGuid();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Mock<IUpdateCrossPostTargetStorage> storage = new();
	private readonly UpdateCrossPostTargetUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public UpdateCrossPostTargetUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(x => x.ScheduleExistsAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(x => x.ExistsAsync(id, scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		sut = new UpdateCrossPostTargetUseCase(storage.Object, identity.Object);
	}

	private static UpdateCrossPostTargetCommand Command(Guid scheduleId, Guid id) =>
		new(scheduleId, id, true, CrossPostFormat.Full, CrossPostLinkTarget.Channel, null, null, false, true, 15);

	[Fact]
	public async Task ThrowScheduleNotFound_WhenScheduleNotOwned()
	{
		storage.Setup(x => x.ScheduleExistsAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<ScheduleNotFoundException>(async () =>
			await sut.Handle(Command(scheduleId, id), CancellationToken.None));
	}

	[Fact]
	public async Task ThrowTargetNotFound_WhenTargetMissing()
	{
		storage.Setup(x => x.ExistsAsync(id, scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<CrossPostTargetNotFoundException>(async () =>
			await sut.Handle(Command(scheduleId, id), CancellationToken.None));

		storage.Verify(
			x => x.UpdateAsync(It.IsAny<UpdateCrossPostTargetCommand>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task UpdateTarget()
	{
		await sut.Handle(Command(scheduleId, id), CancellationToken.None);

		storage.Verify(
			x => x.UpdateAsync(
				It.Is<UpdateCrossPostTargetCommand>(c => c.ScheduleId == scheduleId && c.Id == id),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}
}

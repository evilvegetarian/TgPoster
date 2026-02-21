using Moq;
using Moq.Language.Flow;
using Security.IdentityServices;
using Shouldly;
using TgPoster.API.Domain.Exceptions;
using TgPoster.API.Domain.UseCases.Schedules.DeleteSchedule;

namespace TgPoster.API.Domain.Tests.Schedule;

public class DeleteScheduleUseCaseShould
{
	private readonly Mock<IDeleteScheduleStorage> storage;
	private readonly ISetup<IDeleteScheduleStorage, Task<bool>> existsSetup;
	private readonly DeleteScheduleUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public DeleteScheduleUseCaseShould()
	{
		storage = new Mock<IDeleteScheduleStorage>();
		var identityProvider = new Mock<IIdentityProvider>();

		var identity = new Identity(userId);
		identityProvider.Setup(x => x.Current).Returns(identity);

		existsSetup = storage.Setup(s =>
			s.ScheduleExistAsync(It.IsAny<Guid>(), userId));

		sut = new DeleteScheduleUseCase(storage.Object, identityProvider.Object);
	}

	[Fact]
	public async Task DeleteSchedule_WhenExistsAndBelongsToUser()
	{
		var scheduleId = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		await sut.Handle(new DeleteScheduleCommand(scheduleId), CancellationToken.None);

		storage.Verify(s => s.DeleteScheduleAsync(scheduleId), Times.Once);
	}

	[Fact]
	public async Task ThrowScheduleNotFoundException_WhenNotExists()
	{
		var scheduleId = Guid.NewGuid();
		existsSetup.ReturnsAsync(false);

		await Should.ThrowAsync<ScheduleNotFoundException>(
			async () => await sut.Handle(new DeleteScheduleCommand(scheduleId), CancellationToken.None));

		storage.Verify(s => s.DeleteScheduleAsync(It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	public async Task CheckOwnership_BeforeDeleting()
	{
		var scheduleId = Guid.NewGuid();
		existsSetup.ReturnsAsync(true);

		await sut.Handle(new DeleteScheduleCommand(scheduleId), CancellationToken.None);

		storage.Verify(s => s.ScheduleExistAsync(scheduleId, userId), Times.Once);
	}
}

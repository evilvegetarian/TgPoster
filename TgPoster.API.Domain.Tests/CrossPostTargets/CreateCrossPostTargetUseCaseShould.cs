using Moq;
using Security.IdentityServices;
using Shared.Enums;
using Shouldly;
using TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;
using TgPoster.Exceptions.BadRequest;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.CrossPostTargets;

public class CreateCrossPostTargetUseCaseShould
{
	private readonly Guid accountId = Guid.NewGuid();
	private readonly Guid scheduleId = Guid.NewGuid();
	private readonly Mock<ICreateCrossPostTargetStorage> storage = new();
	private readonly CreateCrossPostTargetUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public CreateCrossPostTargetUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));

		storage.Setup(x => x.ScheduleExistsAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(x => x.SocialAccountExistsAsync(accountId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		sut = new CreateCrossPostTargetUseCase(storage.Object, identity.Object);
	}

	private static CreateCrossPostTargetCommand Command(Guid scheduleId, Guid accountId) =>
		new(scheduleId, accountId, CrossPostFormat.Teaser, CrossPostLinkTarget.Post, null, null, true, false, 0);

	[Fact]
	public async Task ThrowScheduleNotFound_WhenScheduleNotOwned()
	{
		storage.Setup(x => x.ScheduleExistsAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<ScheduleNotFoundException>(async () =>
			await sut.Handle(Command(scheduleId, accountId), CancellationToken.None));
	}

	[Fact]
	public async Task ThrowSocialAccountNotFound_WhenAccountNotOwned()
	{
		storage.Setup(x => x.SocialAccountExistsAsync(accountId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		await Should.ThrowAsync<SocialAccountNotFoundException>(async () =>
			await sut.Handle(Command(scheduleId, accountId), CancellationToken.None));
	}

	[Fact]
	public async Task ThrowAlreadyExists_WhenTargetExists()
	{
		storage.Setup(x => x.TargetExistsAsync(scheduleId, accountId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		await Should.ThrowAsync<CrossPostTargetAlreadyExistsException>(async () =>
			await sut.Handle(Command(scheduleId, accountId), CancellationToken.None));

		storage.Verify(
			x => x.CreateAsync(It.IsAny<CreateCrossPostTargetCommand>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task CreateTarget_AndReturnId()
	{
		var createdId = Guid.NewGuid();
		storage.Setup(x => x.CreateAsync(It.IsAny<CreateCrossPostTargetCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(createdId);

		var response = await sut.Handle(Command(scheduleId, accountId), CancellationToken.None);

		response.Id.ShouldBe(createdId);
		storage.Verify(
			x => x.CreateAsync(
				It.Is<CreateCrossPostTargetCommand>(c => c.ScheduleId == scheduleId && c.SocialAccountId == accountId),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}
}

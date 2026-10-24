using Moq;
using Security.IdentityServices;
using Shared.Services;
using Shouldly;
using TgPoster.API.Domain.UseCases.Messages.GetTime;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.Tests.Messages;

public sealed class GetTimeUseCaseShould
{
	private readonly Mock<IGetTimeStorage> storage = new();
	private readonly GetTimeUseCase sut;
	private readonly Guid userId = Guid.NewGuid();

	public GetTimeUseCaseShould()
	{
		var identity = new Mock<IIdentityProvider>();
		identity.Setup(x => x.Current).Returns(new Identity(userId));
		sut = new GetTimeUseCase(storage.Object, new TimePostingService(), identity.Object);
	}

	[Fact]
	public async Task ReturnPostingTimes_WhenScheduleBelongsToUser()
	{
		var scheduleId = Guid.NewGuid();
		storage.Setup(x => x.ExistScheduleAsync(scheduleId, userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		storage.Setup(x => x.GetScheduleTimeAsync(scheduleId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Dictionary<DayOfWeek, List<TimeOnly>>
			{
				[DayOfWeek.Monday] = [new TimeOnly(10, 0)]
			});

		var response = await sut.Handle(new GetTimeCommand(scheduleId), CancellationToken.None);

		response.PostingTimes.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task ThrowNotFound_WhenScheduleBelongsToAnotherUser()
	{
		var scheduleId = Guid.NewGuid();

		await Should.ThrowAsync<ScheduleNotFoundException>(() =>
			sut.Handle(new GetTimeCommand(scheduleId), CancellationToken.None));

		storage.Verify(x => x.GetScheduleTimeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
	}
}

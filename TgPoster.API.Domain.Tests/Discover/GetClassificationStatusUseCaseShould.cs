using Moq;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.GetClassificationStatus;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

namespace TgPoster.API.Domain.Tests.Discover;

public class GetClassificationStatusUseCaseShould
{
	private readonly Mock<IGetClassificationStatusStorage> storage;
	private readonly GetClassificationStatusUseCase sut;

	public GetClassificationStatusUseCaseShould()
	{
		storage = new Mock<IGetClassificationStatusStorage>();
		sut = new GetClassificationStatusUseCase(storage.Object);
	}

	[Fact]
	public async Task ReturnIdle_WhenStateNotExists()
	{
		storage.Setup(s => s.GetClassificationJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((WorkerJobStateDto?)null);

		var result = await sut.Handle(new GetClassificationStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Idle);
		result.NextRunAt.ShouldBeNull();
	}

	[Fact]
	public async Task ReturnRunningWithProgress_WhenHeartbeatIsFresh()
	{
		var state = CreateState(WorkerJobStateStatus.Running) with
		{
			HeartbeatAt = DateTimeOffset.UtcNow.AddSeconds(-10),
			ProgressCurrent = 1,
			ProgressTotal = 2,
			ProgressMessage = "@channel"
		};
		storage.Setup(s => s.GetClassificationJobStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);

		var result = await sut.Handle(new GetClassificationStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Running);
		result.ProgressCurrent.ShouldBe(1);
		result.ProgressTotal.ShouldBe(2);
		result.ProgressMessage.ShouldBe("@channel");
	}

	[Fact]
	public async Task ReturnUnknown_WhenRunningAndHeartbeatIsStale()
	{
		var state = CreateState(WorkerJobStateStatus.Running) with
		{
			HeartbeatAt = DateTimeOffset.UtcNow.AddMinutes(-30)
		};
		storage.Setup(s => s.GetClassificationJobStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);

		var result = await sut.Handle(new GetClassificationStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Unknown);
	}

	[Fact]
	public async Task ReturnFailedWithError_WhenStatusIsFailed()
	{
		var nextRunAt = DateTimeOffset.UtcNow.AddMinutes(20);
		var state = CreateState(WorkerJobStateStatus.Failed) with
		{
			LastError = "Не задан ключ OpenRouter",
			NextRunAt = nextRunAt
		};
		storage.Setup(s => s.GetClassificationJobStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);

		var result = await sut.Handle(new GetClassificationStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Failed);
		result.LastError.ShouldBe("Не задан ключ OpenRouter");
		result.NextRunAt.ShouldBe(nextRunAt);
	}

	private static WorkerJobStateDto CreateState(WorkerJobStateStatus status) => new(
		status,
		null,
		null,
		null,
		null,
		null,
		null,
		null,
		null,
		null);
}

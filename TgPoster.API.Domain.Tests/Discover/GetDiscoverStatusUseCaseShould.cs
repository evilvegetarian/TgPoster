using Moq;
using Shouldly;
using TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

namespace TgPoster.API.Domain.Tests.Discover;

public class GetDiscoverStatusUseCaseShould
{
	private readonly Mock<IGetDiscoverStatusStorage> storage;
	private readonly GetDiscoverStatusUseCase sut;

	public GetDiscoverStatusUseCaseShould()
	{
		storage = new Mock<IGetDiscoverStatusStorage>();
		sut = new GetDiscoverStatusUseCase(storage.Object);
	}

	[Fact]
	public async Task ReturnIdle_WhenStateNotExists()
	{
		storage.Setup(s => s.GetDiscoverJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync((WorkerJobStateDto?)null);

		var result = await sut.Handle(new GetDiscoverStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Idle);
		result.NextRunAt.ShouldBeNull();
	}

	[Fact]
	public async Task ReturnRunningWithProgress_WhenHeartbeatIsFresh()
	{
		var state = CreateState(WorkerJobStateStatus.Running) with
		{
			HeartbeatAt = DateTimeOffset.UtcNow.AddSeconds(-30),
			ProgressCurrent = 1,
			ProgressTotal = 3,
			ProgressMessage = "@somechannel"
		};
		storage.Setup(s => s.GetDiscoverJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(state);

		var result = await sut.Handle(new GetDiscoverStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Running);
		result.ProgressCurrent.ShouldBe(1);
		result.ProgressTotal.ShouldBe(3);
		result.ProgressMessage.ShouldBe("@somechannel");
	}

	[Fact]
	public async Task ReturnUnknown_WhenRunningAndHeartbeatIsStale()
	{
		var state = CreateState(WorkerJobStateStatus.Running) with
		{
			HeartbeatAt = DateTimeOffset.UtcNow.AddMinutes(-10),
			ProgressCurrent = 1,
			ProgressTotal = 3
		};
		storage.Setup(s => s.GetDiscoverJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(state);

		var result = await sut.Handle(new GetDiscoverStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Unknown);
		result.ProgressCurrent.ShouldBeNull();
		result.ProgressTotal.ShouldBeNull();
	}

	[Fact]
	public async Task ReturnUnknown_WhenRunningAndHeartbeatIsNull()
	{
		var state = CreateState(WorkerJobStateStatus.Running);
		storage.Setup(s => s.GetDiscoverJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(state);

		var result = await sut.Handle(new GetDiscoverStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Unknown);
	}

	[Fact]
	public async Task ReturnCooldownWait_WhenCooldownIsInFuture()
	{
		var cooldownUntil = DateTimeOffset.UtcNow.AddMinutes(15);
		var state = CreateState(WorkerJobStateStatus.CooldownWait) with { CooldownUntil = cooldownUntil };
		storage.Setup(s => s.GetDiscoverJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(state);

		var result = await sut.Handle(new GetDiscoverStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.CooldownWait);
		result.CooldownUntil.ShouldBe(cooldownUntil);
	}

	[Fact]
	public async Task ReturnIdle_WhenCooldownIsInPast()
	{
		var state = CreateState(WorkerJobStateStatus.CooldownWait) with
		{
			CooldownUntil = DateTimeOffset.UtcNow.AddMinutes(-5)
		};
		storage.Setup(s => s.GetDiscoverJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(state);

		var result = await sut.Handle(new GetDiscoverStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Idle);
		result.CooldownUntil.ShouldBeNull();
	}

	[Fact]
	public async Task ReturnFailedWithError_WhenStatusIsFailed()
	{
		var state = CreateState(WorkerJobStateStatus.Failed) with { LastError = "Boom" };
		storage.Setup(s => s.GetDiscoverJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(state);

		var result = await sut.Handle(new GetDiscoverStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Failed);
		result.LastError.ShouldBe("Boom");
	}

	[Fact]
	public async Task ReturnIdleWithSchedule_WhenStatusIsIdle()
	{
		var nextRunAt = DateTimeOffset.UtcNow.AddHours(1);
		var lastFinishedAt = DateTimeOffset.UtcNow.AddHours(-1);
		var state = CreateState(WorkerJobStateStatus.Idle) with
		{
			NextRunAt = nextRunAt,
			LastFinishedAt = lastFinishedAt,
			LastError = "old error"
		};
		storage.Setup(s => s.GetDiscoverJobStateAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(state);

		var result = await sut.Handle(new GetDiscoverStatusQuery(), CancellationToken.None);

		result.Status.ShouldBe(DiscoverJobStatus.Idle);
		result.NextRunAt.ShouldBe(nextRunAt);
		result.LastFinishedAt.ShouldBe(lastFinishedAt);
		result.LastError.ShouldBeNull();
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
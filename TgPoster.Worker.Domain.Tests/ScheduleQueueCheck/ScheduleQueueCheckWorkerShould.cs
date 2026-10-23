using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TgPoster.Worker.Domain.Services.ScheduleAlerts;
using TgPoster.Worker.Domain.UseCases.ScheduleQueueCheck;

namespace TgPoster.Worker.Domain.Tests.ScheduleQueueCheck;

public sealed class ScheduleQueueCheckWorkerShould
{
	private static readonly DateTimeOffset FixedNow = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IScheduleQueueCheckStorage> storage = new();
	private readonly Mock<IScheduleAlertService> alertService = new();
	private readonly Guid scheduleId = Guid.NewGuid();

	[Fact]
	public async Task NotifyQueueEmpty_WhenNoPlannedPosts()
	{
		SetupQueue(new ScheduleQueueInfo(scheduleId, 0, null, 0));

		await CreateSut().CheckAsync();

		alertService.Verify(a => a.NotifyAsync(scheduleId, ScheduleProblem.QueueEmpty, "В очереди нет постов", null,
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NotifyQueueRunningLow_WhenPostsLastTenHours()
	{
		SetupQueue(new ScheduleQueueInfo(scheduleId, 3, FixedNow.AddHours(10), 0));

		await CreateSut().CheckAsync();

		alertService.Verify(a => a.NotifyAsync(scheduleId, ScheduleProblem.QueueRunningLow,
			"Осталось постов: 3, хватит примерно на 0 д 10 ч", null, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task NotNotify_WhenPostsLastFiveDays()
	{
		SetupQueue(new ScheduleQueueInfo(scheduleId, 20, FixedNow.AddDays(5), 0));

		await CreateSut().CheckAsync();

		alertService.Verify(a => a.NotifyAsync(It.IsAny<Guid>(), It.IsAny<ScheduleProblem>(), It.IsAny<string>(),
			It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task MentionPostsAwaitingApproval_WhenQueueEmpty()
	{
		SetupQueue(new ScheduleQueueInfo(scheduleId, 0, null, 2));

		await CreateSut().CheckAsync();

		alertService.Verify(a => a.NotifyAsync(scheduleId, ScheduleProblem.QueueEmpty,
			"В очереди нет постов, ещё 2 ждут подтверждения", null, It.IsAny<CancellationToken>()), Times.Once);
	}

	private void SetupQueue(ScheduleQueueInfo queue)
	{
		storage.Setup(s => s.GetActiveSchedulesQueueAsync(FixedNow, It.IsAny<CancellationToken>()))
			.ReturnsAsync([queue]);
	}

	private ScheduleQueueCheckWorker CreateSut()
	{
		var lifetime = new Mock<IHostApplicationLifetime>();
		lifetime.SetupGet(l => l.ApplicationStopping).Returns(CancellationToken.None);

		return new ScheduleQueueCheckWorker(
			storage.Object,
			alertService.Object,
			new FixedTimeProvider(FixedNow),
			NullLogger<ScheduleQueueCheckWorker>.Instance,
			lifetime.Object);
	}

	private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
	{
		public override DateTimeOffset GetUtcNow() => now;
	}
}

using Microsoft.EntityFrameworkCore;
using Shouldly;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Storage.Storages;

namespace TgPoster.Storage.Tests.Tests;

public sealed class WorkerJobStatusStorageShould(StorageTestFixture fixture)
	: IClassFixture<StorageTestFixture>
{
	private readonly PosterContext context = fixture.GetDbContext();
	private readonly WorkerJobStatusStorage sut = new(fixture.GetDbContext(), new GuidFactory());

	[Fact]
	public async Task EnsureRegisteredAsync_WhenJobIsNew_ShouldCreateIdleState()
	{
		var jobName = NewJobName();
		var nextRunAt = DateTimeOffset.UtcNow.AddHours(2);

		await sut.EnsureRegisteredAsync(jobName, nextRunAt, CancellationToken.None);

		var saved = await context.WorkerJobStates.FirstAsync(x => x.JobName == jobName, CancellationToken.None);
		saved.Status.ShouldBe(WorkerJobStatus.Idle);
		saved.NextRunAt.ShouldBe(nextRunAt);
	}

	[Fact]
	public async Task EnsureRegisteredAsync_WhenJobIsRunning_ShouldNotResetStatus()
	{
		var jobName = NewJobName();
		context.WorkerJobStates.Add(new WorkerJobState
		{
			Id = Guid.NewGuid(),
			JobName = jobName,
			Status = WorkerJobStatus.Running
		});
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();
		var nextRunAt = DateTimeOffset.UtcNow.AddHours(2);

		await sut.EnsureRegisteredAsync(jobName, nextRunAt, CancellationToken.None);

		var saved = await context.WorkerJobStates.FirstAsync(x => x.JobName == jobName, CancellationToken.None);
		saved.Status.ShouldBe(WorkerJobStatus.Running);
		saved.NextRunAt.ShouldBe(nextRunAt);
	}

	[Fact]
	public async Task EnsureRegisteredAsync_WhenCalledTwice_ShouldNotDuplicate()
	{
		var jobName = NewJobName();

		await sut.EnsureRegisteredAsync(jobName, null, CancellationToken.None);
		await sut.EnsureRegisteredAsync(jobName, null, CancellationToken.None);

		var count = await context.WorkerJobStates.CountAsync(x => x.JobName == jobName, CancellationToken.None);
		count.ShouldBe(1);
	}

	[Fact]
	public async Task ReportStartedAsync_ShouldSetRunningAndResetPreviousRun()
	{
		var jobName = NewJobName();
		context.WorkerJobStates.Add(new WorkerJobState
		{
			Id = Guid.NewGuid(),
			JobName = jobName,
			Status = WorkerJobStatus.Failed,
			LastError = "old error",
			CooldownUntil = DateTimeOffset.UtcNow.AddMinutes(10),
			ProgressCurrent = 5,
			ProgressTotal = 10,
			ProgressMessage = "old progress"
		});
		await context.SaveChangesAsync(CancellationToken.None);
		context.ChangeTracker.Clear();

		await sut.ReportStartedAsync(jobName, CancellationToken.None);

		var saved = await context.WorkerJobStates.FirstAsync(x => x.JobName == jobName, CancellationToken.None);
		saved.Status.ShouldBe(WorkerJobStatus.Running);
		saved.LastStartedAt.ShouldNotBeNull();
		saved.HeartbeatAt.ShouldNotBeNull();
		saved.LastError.ShouldBeNull();
		saved.CooldownUntil.ShouldBeNull();
		saved.ProgressCurrent.ShouldBeNull();
		saved.ProgressTotal.ShouldBeNull();
		saved.ProgressMessage.ShouldBeNull();
	}

	[Fact]
	public async Task ReportHeartbeatAsync_ShouldUpdateProgress()
	{
		var jobName = NewJobName();
		await sut.ReportStartedAsync(jobName, CancellationToken.None);

		await sut.ReportHeartbeatAsync(jobName, 2, 5, "@channel: загружено 300 сообщений", CancellationToken.None);

		var saved = await context.WorkerJobStates.FirstAsync(x => x.JobName == jobName, CancellationToken.None);
		saved.HeartbeatAt.ShouldNotBeNull();
		saved.ProgressCurrent.ShouldBe(2);
		saved.ProgressTotal.ShouldBe(5);
		saved.ProgressMessage.ShouldBe("@channel: загружено 300 сообщений");
	}

	[Fact]
	public async Task ReportCooldownAsync_ShouldSetCooldownWait()
	{
		var jobName = NewJobName();
		await sut.ReportStartedAsync(jobName, CancellationToken.None);
		var cooldownUntil = DateTimeOffset.UtcNow.AddMinutes(30);
		var nextRunAt = DateTimeOffset.UtcNow.AddHours(2);

		await sut.ReportCooldownAsync(jobName, cooldownUntil, nextRunAt, CancellationToken.None);

		var saved = await context.WorkerJobStates.FirstAsync(x => x.JobName == jobName, CancellationToken.None);
		saved.Status.ShouldBe(WorkerJobStatus.CooldownWait);
		saved.CooldownUntil.ShouldBe(cooldownUntil);
		saved.NextRunAt.ShouldBe(nextRunAt);
		saved.LastFinishedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task ReportCompletedAsync_ShouldSetIdle()
	{
		var jobName = NewJobName();
		await sut.ReportStartedAsync(jobName, CancellationToken.None);
		var nextRunAt = DateTimeOffset.UtcNow.AddHours(2);

		await sut.ReportCompletedAsync(jobName, nextRunAt, CancellationToken.None);

		var saved = await context.WorkerJobStates.FirstAsync(x => x.JobName == jobName, CancellationToken.None);
		saved.Status.ShouldBe(WorkerJobStatus.Idle);
		saved.NextRunAt.ShouldBe(nextRunAt);
		saved.LastFinishedAt.ShouldNotBeNull();
	}

	[Fact]
	public async Task ReportFailedAsync_ShouldSetFailedAndTruncateLongError()
	{
		var jobName = NewJobName();
		await sut.ReportStartedAsync(jobName, CancellationToken.None);
		var longError = new string('x', 3000);

		await sut.ReportFailedAsync(jobName, longError, null, CancellationToken.None);

		var saved = await context.WorkerJobStates.FirstAsync(x => x.JobName == jobName, CancellationToken.None);
		saved.Status.ShouldBe(WorkerJobStatus.Failed);
		saved.LastError.ShouldNotBeNull();
		saved.LastError.Length.ShouldBe(2000);
		saved.LastFinishedAt.ShouldNotBeNull();
	}

	private static string NewJobName() => $"test-job-{Guid.NewGuid():N}";
}

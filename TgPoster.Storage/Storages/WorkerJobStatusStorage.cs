using Microsoft.EntityFrameworkCore;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;
using TgPoster.Worker.Domain.UseCases.WorkerJobStatus;

namespace TgPoster.Storage.Storages;

internal sealed class WorkerJobStatusStorage(PosterContext context, GuidFactory guidFactory)
	: IWorkerJobStatusStorage
{
	public async Task EnsureRegisteredAsync(string jobName, DateTimeOffset? nextRunAt, CancellationToken ct)
	{
		var state = await GetOrCreateAsync(jobName, ct);
		if (nextRunAt is not null)
		{
			state.NextRunAt = nextRunAt;
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task ReportStartedAsync(string jobName, CancellationToken ct)
	{
		var state = await GetOrCreateAsync(jobName, ct);
		state.Status = WorkerJobStatus.Running;
		state.LastStartedAt = DateTimeOffset.UtcNow;
		state.HeartbeatAt = state.LastStartedAt;
		state.LastError = null;
		state.CooldownUntil = null;
		state.ProgressCurrent = null;
		state.ProgressTotal = null;
		state.ProgressMessage = null;

		await context.SaveChangesAsync(ct);
	}

	public async Task ReportHeartbeatAsync(string jobName, int? current, int? total, string? message,
		CancellationToken ct)
	{
		var state = await GetOrCreateAsync(jobName, ct);
		state.HeartbeatAt = DateTimeOffset.UtcNow;
		state.ProgressCurrent = current;
		state.ProgressTotal = total;
		state.ProgressMessage = Truncate(message, 512);

		await context.SaveChangesAsync(ct);
	}

	public async Task ReportCooldownAsync(string jobName, DateTimeOffset cooldownUntil, DateTimeOffset? nextRunAt,
		CancellationToken ct)
	{
		var state = await GetOrCreateAsync(jobName, ct);
		state.Status = WorkerJobStatus.CooldownWait;
		state.CooldownUntil = cooldownUntil;
		state.LastFinishedAt = DateTimeOffset.UtcNow;
		state.HeartbeatAt = state.LastFinishedAt;
		if (nextRunAt is not null)
		{
			state.NextRunAt = nextRunAt;
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task ReportCompletedAsync(string jobName, DateTimeOffset? nextRunAt, CancellationToken ct)
	{
		var state = await GetOrCreateAsync(jobName, ct);
		state.Status = WorkerJobStatus.Idle;
		state.LastFinishedAt = DateTimeOffset.UtcNow;
		state.HeartbeatAt = state.LastFinishedAt;
		if (nextRunAt is not null)
		{
			state.NextRunAt = nextRunAt;
		}

		await context.SaveChangesAsync(ct);
	}

	public async Task ReportFailedAsync(string jobName, string error, DateTimeOffset? nextRunAt, CancellationToken ct)
	{
		var state = await GetOrCreateAsync(jobName, ct);
		state.Status = WorkerJobStatus.Failed;
		state.LastError = Truncate(error, 2000);
		state.LastFinishedAt = DateTimeOffset.UtcNow;
		state.HeartbeatAt = state.LastFinishedAt;
		if (nextRunAt is not null)
		{
			state.NextRunAt = nextRunAt;
		}

		await context.SaveChangesAsync(ct);
	}

	private async Task<WorkerJobState> GetOrCreateAsync(string jobName, CancellationToken ct)
	{
		var state = await context.WorkerJobStates.FirstOrDefaultAsync(x => x.JobName == jobName, ct);
		if (state is null)
		{
			state = new WorkerJobState
			{
				Id = guidFactory.New(),
				JobName = jobName,
				Status = WorkerJobStatus.Idle
			};
			context.WorkerJobStates.Add(state);
		}

		return state;
	}

	private static string? Truncate(string? value, int maxLength) =>
		value?.Length > maxLength ? value[..maxLength] : value;
}

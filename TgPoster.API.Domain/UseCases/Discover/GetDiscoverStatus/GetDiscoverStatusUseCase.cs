using MediatR;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverStatus;

internal sealed class GetDiscoverStatusUseCase(IGetDiscoverStatusStorage storage)
	: IRequestHandler<GetDiscoverStatusQuery, DiscoverStatusResponse>
{
	/// <summary>
	///     Если воркер не подавал признаков жизни дольше этого интервала при статусе Running,
	///     считаем состояние неизвестным (процесс убит или завис)
	/// </summary>
	private static readonly TimeSpan StaleHeartbeat = TimeSpan.FromMinutes(5);

	public async Task<DiscoverStatusResponse> Handle(GetDiscoverStatusQuery request, CancellationToken ct)
	{
		var state = await storage.GetDiscoverJobStateAsync(ct);
		if (state is null)
		{
			return new DiscoverStatusResponse { Status = DiscoverJobStatus.Idle };
		}

		var now = DateTimeOffset.UtcNow;
		var status = state.Status switch
		{
			WorkerJobStateStatus.Running when state.HeartbeatAt is null || now - state.HeartbeatAt > StaleHeartbeat
				=> DiscoverJobStatus.Unknown,
			WorkerJobStateStatus.Running => DiscoverJobStatus.Running,
			WorkerJobStateStatus.CooldownWait when state.CooldownUntil is null || state.CooldownUntil <= now
				=> DiscoverJobStatus.Idle,
			WorkerJobStateStatus.CooldownWait => DiscoverJobStatus.CooldownWait,
			WorkerJobStateStatus.Failed => DiscoverJobStatus.Failed,
			_ => DiscoverJobStatus.Idle
		};

		return new DiscoverStatusResponse
		{
			Status = status,
			LastStartedAt = state.LastStartedAt,
			LastFinishedAt = state.LastFinishedAt,
			NextRunAt = state.NextRunAt,
			HeartbeatAt = state.HeartbeatAt,
			CooldownUntil = status == DiscoverJobStatus.CooldownWait ? state.CooldownUntil : null,
			LastError = status == DiscoverJobStatus.Failed ? state.LastError : null,
			ProgressCurrent = status == DiscoverJobStatus.Running ? state.ProgressCurrent : null,
			ProgressTotal = status == DiscoverJobStatus.Running ? state.ProgressTotal : null,
			ProgressMessage = status == DiscoverJobStatus.Running ? state.ProgressMessage : null
		};
	}
}
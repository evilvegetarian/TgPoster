using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;

internal sealed class GetChannelCleanupJobUseCase(
	IGetChannelCleanupJobStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<GetChannelCleanupJobQuery, ChannelCleanupJobResponse>
{
	public async Task<ChannelCleanupJobResponse> Handle(GetChannelCleanupJobQuery request, CancellationToken ct)
	{
		var job = await storage.GetJobStateAsync(request.JobId, identity.Current.UserId, ct);
		if (job == null)
		{
			throw new ChannelCleanupJobNotFoundException(request.JobId);
		}

		var retryAfterSeconds = job.Status == ChannelCleanupStatus.CooldownWait
			? GetRetryAfterSeconds(job.SessionFloodWaitUntil)
			: null;

		return ChannelCleanupJobResponse.Create(job.JobId, job.Status, retryAfterSeconds, job.LastError, job.Items);
	}

	private static int? GetRetryAfterSeconds(DateTimeOffset? floodWaitUntil)
	{
		if (floodWaitUntil == null)
		{
			return null;
		}

		var seconds = (int)Math.Ceiling((floodWaitUntil.Value - DateTimeOffset.UtcNow).TotalSeconds);

		return seconds > 0 ? seconds : null;
	}
}

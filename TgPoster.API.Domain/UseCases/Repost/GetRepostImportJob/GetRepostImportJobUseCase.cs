using MediatR;
using Security.IdentityServices;
using Shared.Enums;
using TgPoster.Exceptions.NotFound;

namespace TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;

internal sealed class GetRepostImportJobUseCase(
	IGetRepostImportJobStorage storage,
	IIdentityProvider identity)
	: IRequestHandler<GetRepostImportJobQuery, RepostImportJobResponse>
{
	public async Task<RepostImportJobResponse> Handle(GetRepostImportJobQuery request, CancellationToken ct)
	{
		var job = await storage.GetJobStateAsync(request.JobId, identity.Current.UserId, ct);
		if (job == null)
		{
			throw new RepostImportJobNotFoundException(request.JobId);
		}

		var addedCount = job.Items.Count(x => x.Outcome == AddDestinationOutcome.Added);
		var pendingCount = job.Items.Count(x => x.Outcome == AddDestinationOutcome.Pending);

		return new RepostImportJobResponse
		{
			JobId = job.JobId,
			Status = job.Status,
			TotalCount = job.Items.Count,
			AddedCount = addedCount,
			SkippedCount = job.Items.Count - addedCount - pendingCount,
			PendingCount = pendingCount,
			RetryAfterSeconds = job.Status == RepostImportStatus.CooldownWait
				? GetRetryAfterSeconds(job.SessionFloodWaitUntil)
				: null,
			Results = job.Items
				.Select(x => new AddDestinationResultDto
				{
					DiscoveredChannelId = x.DiscoveredChannelId,
					Title = x.Title,
					Outcome = x.Outcome,
					DestinationId = x.RepostDestinationId,
					Error = x.Error
				})
				.ToList()
		};
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

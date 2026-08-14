using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.GetRepostImportJob;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class GetRepostImportJobStorage(PosterContext context) : IGetRepostImportJobStorage
{
	public Task<RepostImportJobState?> GetJobStateAsync(Guid jobId, Guid userId, CancellationToken ct)
	{
		return context.RepostImportJobs
			.Where(x => x.Id == jobId && x.RepostSettings.Schedule.UserId == userId)
			.Select(x => new RepostImportJobState(
				x.Id,
				x.Status,
				x.RepostSettings.TelegramSession.FloodWaitUntil,
				x.Items
					.OrderBy(i => i.Order)
					.Select(i => new RepostImportJobItemState(
						i.DiscoveredChannelId,
						i.Title,
						i.Outcome,
						i.RepostDestinationId,
						i.Error))
					.ToList()))
			.FirstOrDefaultAsync(ct);
	}
}

using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.ChannelCleanup.GetChannelCleanupJob;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.ChannelCleanup;

internal sealed class GetChannelCleanupJobStorage(PosterContext context) : IGetChannelCleanupJobStorage
{
	public Task<ChannelCleanupJobState?> GetJobStateAsync(Guid jobId, Guid userId, CancellationToken ct)
	{
		return context.ChannelCleanupJobs
			.Where(x => x.Id == jobId && x.TelegramSession.UserId == userId)
			.Select(x => new ChannelCleanupJobState(
				x.Id,
				x.Status,
				x.TelegramSession.FloodWaitUntil,
				x.LastError,
				x.Items
					.OrderBy(i => i.Order)
					.Select(i => new ChannelCleanupItemResponse
					{
						ChatId = i.ChatId,
						Title = i.Title,
						Username = i.Username,
						ChatType = i.ChatType,
						Outcome = i.Outcome,
						Error = i.Error
					})
					.ToList()))
			.FirstOrDefaultAsync(ct);
	}
}

using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class UpdateRepostDestinationStorage(PosterContext context) : IUpdateRepostDestinationStorage
{
	public Task<RepostDestinationInfo?> GetDestinationAsync(Guid id, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.Where(x => x.Id == id)
			.Select(x => new RepostDestinationInfo(
				x.RepostSettings.TelegramSessionId,
				x.ChatId,
				x.Username,
				x.IsActive,
				x.ChatStatus))
			.FirstOrDefaultAsync(ct);
	}

	public async Task UpdateDestinationAsync(
		Guid id,
		bool isActive,
		ChatStatus chatStatus,
		int delayMinSeconds,
		int delayMaxSeconds,
		int repostEveryNth,
		int skipProbability,
		int? maxRepostsPerDay,
		CancellationToken ct
	)
	{
		var destination = await context.Set<RepostDestination>()
			.FirstAsync(x => x.Id == id, ct);

		destination.IsActive = isActive;
		destination.ChatStatus = chatStatus;
		destination.DelayMinSeconds = delayMinSeconds;
		destination.DelayMaxSeconds = delayMaxSeconds;
		destination.RepostEveryNth = repostEveryNth;
		destination.SkipProbability = skipProbability;
		destination.MaxRepostsPerDay = maxRepostsPerDay;

		await context.SaveChangesAsync(ct);
	}
}

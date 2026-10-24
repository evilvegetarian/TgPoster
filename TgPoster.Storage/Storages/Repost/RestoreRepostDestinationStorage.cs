using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.RestoreRepostDestination;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class RestoreRepostDestinationStorage(PosterContext context) : IRestoreRepostDestinationStorage
{
	public Task<bool> SettingsExistsAsync(Guid settingsId, Guid userId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.AnyAsync(x => x.Id == settingsId && x.Schedule.UserId == userId, ct);
	}

	public Task<RemovedRepostDestinationDto?> GetRemovedAsync(
		Guid settingsId,
		Guid destinationId,
		CancellationToken ct
	)
	{
		return context.Set<RepostDestination>()
			.IgnoreQueryFilters()
			.Where(x => x.Id == destinationId
			            && x.RepostSettingsId == settingsId
			            && x.Deleted != null
			            && x.PurgedAt == null)
			.Select(x => new RemovedRepostDestinationDto(x.ChatId, x.Title))
			.FirstOrDefaultAsync(ct);
	}

	public Task<bool> ChatInListAsync(Guid settingsId, long chatId, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.AnyAsync(x => x.RepostSettingsId == settingsId && x.ChatId == chatId, ct);
	}

	public async Task RestoreAsync(Guid destinationId, CancellationToken ct)
	{
		var destination = await context.Set<RepostDestination>()
			.IgnoreQueryFilters()
			.FirstAsync(x => x.Id == destinationId, ct);

		destination.Deleted = null;
		destination.DeletedById = null;
		destination.IsActive = false;

		await context.SaveChangesAsync(ct);
	}
}

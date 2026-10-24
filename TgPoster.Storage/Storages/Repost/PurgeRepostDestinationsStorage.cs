using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.PurgeRepostDestinations;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class PurgeRepostDestinationsStorage(PosterContext context) : IPurgeRepostDestinationsStorage
{
	public Task<bool> SettingsExistsAsync(Guid settingsId, Guid userId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.AnyAsync(x => x.Id == settingsId && x.Schedule.UserId == userId, ct);
	}

	public Task<List<Guid>> GetPurgeableIdsAsync(Guid settingsId, List<Guid> destinationIds, CancellationToken ct)
	{
		// Удалённые из списка тоже можно стереть насовсем, поэтому фильтр мягкого удаления снят
		return context.Set<RepostDestination>()
			.IgnoreQueryFilters()
			.Where(x => x.RepostSettingsId == settingsId && x.PurgedAt == null && destinationIds.Contains(x.Id))
			.Select(x => x.Id)
			.ToListAsync(ct);
	}

	public async Task PurgeAsync(List<Guid> destinationIds, CancellationToken ct)
	{
		var destinations = await context.Set<RepostDestination>()
			.IgnoreQueryFilters()
			.Where(x => destinationIds.Contains(x.Id))
			.ToListAsync(ct);

		var now = DateTimeOffset.UtcNow;
		foreach (var destination in destinations)
		{
			destination.PurgedAt = now;
			destination.IsActive = false;
			if (destination.Deleted == null)
			{
				context.Remove(destination);
			}
		}

		await context.SaveChangesAsync(ct);
	}
}

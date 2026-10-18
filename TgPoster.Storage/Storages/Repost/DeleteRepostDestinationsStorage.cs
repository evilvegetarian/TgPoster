using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestinations;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class DeleteRepostDestinationsStorage(PosterContext context) : IDeleteRepostDestinationsStorage
{
	public Task<bool> SettingsExistsAsync(Guid settingsId, Guid userId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.AnyAsync(x => x.Id == settingsId && x.Schedule.UserId == userId, ct);
	}

	public Task<List<Guid>> GetExistingIdsAsync(Guid settingsId, List<Guid> destinationIds, CancellationToken ct)
	{
		return context.Set<RepostDestination>()
			.Where(x => x.RepostSettingsId == settingsId && destinationIds.Contains(x.Id))
			.Select(x => x.Id)
			.ToListAsync(ct);
	}

	public async Task DeleteAsync(List<Guid> destinationIds, CancellationToken ct)
	{
		var destinations = await context.Set<RepostDestination>()
			.Where(x => destinationIds.Contains(x.Id))
			.ToListAsync(ct);

		context.RemoveRange(destinations);
		await context.SaveChangesAsync(ct);
	}
}

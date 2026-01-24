using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class UpdateRepostDestinationStorage(PosterContext context) : IUpdateRepostDestinationStorage
{
	public Task<bool> DestinationExistsAsync(Guid id, CancellationToken ct)
	{
		return context.Set<RepostDestination>().AnyAsync(x => x.Id == id, ct);
	}

	public async Task UpdateDestinationAsync(Guid id, bool isActive, CancellationToken ct)
	{
		var destination = await context.Set<RepostDestination>()
			.FirstAsync(x => x.Id == id, ct);

		destination.IsActive = isActive;

		await context.SaveChangesAsync(ct);
	}
}

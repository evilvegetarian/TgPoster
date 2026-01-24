using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestination;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class DeleteRepostDestinationStorage(PosterContext context) : IDeleteRepostDestinationStorage
{
	public Task<bool> DestinationExistsAsync(Guid id, CancellationToken ct)
	{
		return context.Set<RepostDestination>().AnyAsync(x => x.Id == id, ct);
	}

	public async Task DeleteDestinationAsync(Guid id, CancellationToken ct)
	{
		var destination = await context.Set<RepostDestination>()
			.FirstAsync(x => x.Id == id, ct);

		context.Remove(destination);
		await context.SaveChangesAsync(ct);
	}
}

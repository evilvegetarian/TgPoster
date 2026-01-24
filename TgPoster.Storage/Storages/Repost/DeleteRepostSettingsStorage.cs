using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.DeleteRepostSettings;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class DeleteRepostSettingsStorage(PosterContext context) : IDeleteRepostSettingsStorage
{
	public Task<bool> RepostSettingsExistsAsync(Guid id, CancellationToken ct)
	{
		return context.Set<Data.Entities.RepostSettings>().AnyAsync(x => x.Id == id, ct);
	}

	public async Task DeleteRepostSettingsAsync(Guid id, CancellationToken ct)
	{
		var settings = await context.Set<Data.Entities.RepostSettings>()
			.FirstAsync(x => x.Id == id, ct);

		context.Remove(settings);
		await context.SaveChangesAsync(ct);
	}
}

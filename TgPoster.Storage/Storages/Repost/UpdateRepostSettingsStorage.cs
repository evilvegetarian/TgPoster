using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.Repost.UpdateRepostSettings;
using TgPoster.Storage.Data;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages.Repost;

internal sealed class UpdateRepostSettingsStorage(PosterContext context) : IUpdateRepostSettingsStorage
{
	public Task<bool> SettingsExistsAsync(Guid id, Guid userId, CancellationToken ct)
	{
		return context.Set<RepostSettings>()
			.AnyAsync(x => x.Id == id && x.Schedule.UserId == userId, ct);
	}

	public async Task UpdateSettingsAsync(Guid id, bool isActive, CancellationToken ct)
	{
		var settings = await context.Set<RepostSettings>()
			.FirstAsync(x => x.Id == id, ct);

		settings.IsActive = isActive;

		await context.SaveChangesAsync(ct);
	}
}

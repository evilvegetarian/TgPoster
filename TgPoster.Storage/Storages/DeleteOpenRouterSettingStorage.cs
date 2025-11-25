using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.DeleteOpenRouterSetting;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

public class DeleteOpenRouterSettingStorage(PosterContext context) : IDeleteOpenRouterSettingStorage
{
	public Task<bool> ExistsAsync(Guid id, CancellationToken ctx)
	{
		return context.OpenRouterSettings.AnyAsync(x => x.Id == id, ctx);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ctx)
	{
		var entity = await context.OpenRouterSettings.FirstOrDefaultAsync(x => x.Id == id, ctx);
		context.OpenRouterSettings.Remove(entity!);
		await context.SaveChangesAsync(ctx);
	}
}
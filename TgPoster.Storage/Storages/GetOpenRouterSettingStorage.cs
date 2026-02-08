using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class GetOpenRouterSettingStorage(PosterContext context) : IGetOpenRouterSettingStorage
{
	public Task<OpenRouterSettingDto?> Get(Guid id, Guid userId, CancellationToken ctx)
	{
		return context.OpenRouterSettings
			.Where(o => o.Id == id)
			.Where(o => o.UserId == userId)
			.Select(x => new OpenRouterSettingDto
			{
				Id = x.Id,
				Model = x.Model
			})
			.FirstOrDefaultAsync(ctx);
	}
}
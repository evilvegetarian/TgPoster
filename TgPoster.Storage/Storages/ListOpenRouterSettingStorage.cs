using Microsoft.EntityFrameworkCore;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;
using TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;
using TgPoster.Storage.Data;

namespace TgPoster.Storage.Storages;

internal sealed class ListOpenRouterSettingStorage(PosterContext context) : IListOpenRouterSettingStorage
{
	public Task<List<OpenRouterSettingDto>> GetAsync(Guid userId, CancellationToken ct)
	{
		return context.OpenRouterSettings
			.Where(x => x.UserId == userId)
			.Select(x => new OpenRouterSettingDto
			{
				Id = x.Id,
				Model = x.Model
			}).ToListAsync(ct);
	}
}
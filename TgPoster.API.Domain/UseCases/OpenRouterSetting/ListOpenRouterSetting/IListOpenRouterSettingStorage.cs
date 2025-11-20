using TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;

namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.ListOpenRouterSetting;

public interface IListOpenRouterSettingStorage
{
	Task<List<OpenRouterSettingDto>> GetAsync(Guid userId, CancellationToken ct);
}
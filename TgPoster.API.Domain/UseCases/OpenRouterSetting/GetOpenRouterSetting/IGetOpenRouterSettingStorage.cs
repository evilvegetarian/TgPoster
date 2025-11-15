namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.GetOpenRouterSetting;

public interface IGetOpenRouterSettingStorage
{
	public Task<OpenRouterSettingDto?> Get(Guid id, Guid userId, CancellationToken ctx);
}
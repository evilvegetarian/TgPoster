namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.DeleteOpenRouterSetting;

public interface IDeleteOpenRouterSettingStorage
{
	Task<bool> ExistsAsync(Guid id, CancellationToken ctx);
	Task DeleteAsync(Guid id, CancellationToken ctx);
}
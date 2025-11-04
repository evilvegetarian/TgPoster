namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;

public interface ICreateOpenRouterSettingStorage
{
	public Task Create(string token, string model, Guid userId, CancellationToken cancellationToken);
}
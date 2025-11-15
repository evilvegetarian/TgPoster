namespace TgPoster.API.Domain.UseCases.OpenRouterSetting.CreateOpenRouterSetting;

public interface ICreateOpenRouterSettingStorage
{
	public Task<Guid> Create(string token, string model, Guid userId, CancellationToken cancellationToken);
}
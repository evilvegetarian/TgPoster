namespace TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;

public interface IListPromptSettingStorage
{
	Task<List<PromptSettingResponse>> GetAsync(Guid userId, CancellationToken cancellationToken);
}
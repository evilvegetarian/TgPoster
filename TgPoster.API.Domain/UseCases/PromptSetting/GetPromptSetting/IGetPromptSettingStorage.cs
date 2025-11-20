using TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;

namespace TgPoster.API.Domain.UseCases.PromptSetting.GetPromptSetting;

public interface IGetPromptSettingStorage
{
	Task<PromptSettingResponse?> GetAsync(Guid requestId, Guid currentUserId, CancellationToken cancellationToken);
}
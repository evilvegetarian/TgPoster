namespace TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;

public interface ICreatePromptSettingStorage
{
	Task<bool> ExistScheduleAsync(Guid scheduleId, CancellationToken ctx);
	Task CreatePromptSettingAsync(Guid scheduleId, string? requestTextPrompt, string? requestVideoPrompt, string? requestPhotoPrompt, CancellationToken ctx);
}
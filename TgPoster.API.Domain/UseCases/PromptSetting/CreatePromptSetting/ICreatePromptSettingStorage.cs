namespace TgPoster.API.Domain.UseCases.PromptSetting.CreatePromptSetting;

public interface ICreatePromptSettingStorage
{
	Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ctx);

	Task<Guid> CreatePromptSettingAsync(
		Guid scheduleId,
		string? requestTextPrompt,
		string? requestVideoPrompt,
		string? requestPhotoPrompt,
		CancellationToken ctx
	);
}
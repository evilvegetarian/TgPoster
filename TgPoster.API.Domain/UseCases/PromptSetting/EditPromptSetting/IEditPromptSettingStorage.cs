namespace TgPoster.API.Domain.UseCases.PromptSetting.EditPromptSetting;

public interface IEditPromptSettingStorage
{
	Task<bool> ExistPromptAsync(Guid id, Guid userId, CancellationToken ctx);
	Task UpdatePromptAsync(Guid id, string? text, string? video, string? photo, CancellationToken ctx);
}
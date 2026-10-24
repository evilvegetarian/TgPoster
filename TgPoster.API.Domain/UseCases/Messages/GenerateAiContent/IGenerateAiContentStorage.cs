namespace TgPoster.API.Domain.UseCases.Messages.GenerateAiContent;

public interface IGenerateAiContentStorage
{
	Task<OpenRouterDto?> GetOpenRouterAsync(Guid messageId, Guid userId, CancellationToken ct);
	Task<GenerateAiContentMessageDto?> GetMessageAsync(Guid messageId, Guid userId, CancellationToken ct);
	Task<GenerateAiContentPromptSettingDto?> GetPromptSettingsAsync(Guid scheduleId, CancellationToken ct);
}
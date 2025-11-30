namespace TgPoster.API.Domain.UseCases.Messages.GenerateAiContent;

public sealed class GenerateAiContentPromptSettingDto
{
	public string? PhotoPrompt { get; set; }
	public string? TextPrompt { get; set; }
	public string? VideoPrompt { get; set; }
}
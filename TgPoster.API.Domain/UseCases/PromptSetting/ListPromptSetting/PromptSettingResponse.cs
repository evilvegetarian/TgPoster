namespace TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;

public class PromptSettingResponse
{
	/// <summary>
	/// Id Промта
	/// </summary>
	public required Guid Id { get; set; }

	/// <summary>
	/// Промпт для видео
	/// </summary>
	public string? VideoPrompt { get; set; }

	/// <summary>
	/// Промпт для картинок
	/// </summary>
	public string? PicturePrompt { get; set; }

	/// <summary>
	/// Промпт для текста
	/// </summary>
	public string? TextPrompt { get; set; }
}
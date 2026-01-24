namespace TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;

public sealed record PromptSettingResponse
{
	/// <summary>
	///     Id Промта
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Промпт для видео
	/// </summary>
	public string? VideoPrompt { get; init; }

	/// <summary>
	///     Промпт для картинок
	/// </summary>
	public string? PicturePrompt { get; init; }

	/// <summary>
	///     Промпт для текста
	/// </summary>
	public string? TextPrompt { get; init; }
}
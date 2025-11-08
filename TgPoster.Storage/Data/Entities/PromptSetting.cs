namespace TgPoster.Storage.Data.Entities;

public sealed class PromptSetting : BaseEntity
{
	/// <summary>
	/// Промпт для фото
	/// </summary>
	public string? VideoPrompt { get; set; }

	/// <summary>
	/// Промпт для картинок
	/// </summary>
	public string? PicturePrompt { get; set; }

	/// <summary>
	/// Промпт для видео
	/// </summary>
	public string? TextPrompt { get; set; }

	/// <summary>
	/// Владелец OpenRouter
	/// </summary>
	public required Guid ScheduleId { get; set; }

	#region Navigtion

	public Schedule Schedule { get; set; } = null!;

	#endregion
}
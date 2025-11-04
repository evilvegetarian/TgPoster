namespace TgPoster.Storage.Data.Entities;

public sealed class PromptSetting : BaseEntity
{
	/// <summary>
	/// Промпт для фото
	/// </summary>
	public required string VideoPrompt { get; set; }

	/// <summary>
	/// Промпт для картинок
	/// </summary>
	public required string PicturePrompt { get; set; }

	/// <summary>
	/// Промпт для видео
	/// </summary>
	public required string TextPrompt { get; set; }

	/// <summary>
	/// Владелец OpenRouter
	/// </summary>
	public required Guid ScheduleId { get; set; }

	#region Navigtion

	public Schedule Schedule { get; set; } = null!;

	#endregion
}
namespace TgPoster.API.Models;

/// <summary>
/// Request создания промптов
/// </summary>
public class CreatePromptSettingRequest
{
	/// <summary>
	/// Id расписания для которого Промпты
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	/// Промпт для текста
	/// </summary>
	public string? TextPrompt { get; set; }

	/// <summary>
	/// Промпт для видео
	/// </summary>
	public string? VideoPrompt { get; set; }

	/// <summary>
	/// Промпт для фото 
	/// </summary>
	public string? PhotoPrompt { get; set; }
}
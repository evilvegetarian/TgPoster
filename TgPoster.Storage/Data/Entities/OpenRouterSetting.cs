namespace TgPoster.Storage.Data.Entities;

public sealed class OpenRouterSetting : BaseEntity
{
	/// <summary>
	/// Модель ИИ
	/// </summary>
	public required string Model { get; set; }

	/// <summary>
	/// Захэшированый токен телеграмма
	/// </summary>
	public required string TokenHash { get; set; }

	/// <summary>
	/// Владелец OpenRouter
	/// </summary>
	public required Guid UserId { get; set; }

	#region Navigtion

	public User User { get; set; } = null!;
	public ICollection<PromptSetting> PromptSettings { get; set; } = [];

	#endregion
}
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

	/// <summary>
	/// Владелец OpenRouter
	/// </summary>
	public Guid? ScheduleId { get; set; }

	#region Navigtion

	public Schedule? Schedule { get; set; }
	public User User { get; set; } = null!;

	#endregion
}
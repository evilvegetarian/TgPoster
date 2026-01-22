namespace TgPoster.API.Models;

/// <summary>
///     Запрос на обновление Telegram бота
/// </summary>
public class UpdateTelegramBotRequest
{
	/// <summary>
	///     Название телеграм бота
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	///     Активность телеграм бота
	/// </summary>
	public bool IsActive { get; set; }
}
namespace TgPoster.API.Models;

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
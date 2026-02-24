using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на обновление расписания
/// </summary>
public sealed class UpdateScheduleRequest
{
	/// <summary>
	///     Название расписания (опционально)
	/// </summary>
	[MaxLength(100)]
	public string? Name { get; init; }

	/// <summary>
	///     Идентификатор YouTube аккаунта (опционально)
	/// </summary>
	public Guid? YouTubeAccountId { get; init; }

	/// <summary>
	///     Идентификатор Telegram бота (опционально)
	/// </summary>
	public Guid? TelegramBotId { get; init; }
}
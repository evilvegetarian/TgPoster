using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Запрос на создание расписания
/// </summary>
public sealed class CreateScheduleRequest
{
	/// <summary>
	///     Название расписания
	/// </summary>
	[Required]
	[MaxLength(100)]
	public required string Name { get; init; }

	/// <summary>
	///     Название канала для публикации
	/// </summary>
	[Required]
	[MaxLength(100)]
	public required string Channel { get; init; }

	/// <summary>
	///     Идентификатор Telegram бота
	/// </summary>
	[Required]
	public required Guid TelegramBotId { get; init; }

	/// <summary>
	///     Идентификатор YouTube аккаунта (опционально)
	/// </summary>
	public Guid? YouTubeAccountId { get; init; }
}
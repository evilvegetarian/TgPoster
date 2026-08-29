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

	/// <summary>
	///     Общая подпись, приклеиваемая снизу к каждому посту расписания.
	///     Поддерживает HTML-разметку Telegram. Пустая строка очищает подпись, null оставляет её без изменений
	/// </summary>
	[MaxLength(1024)]
	public string? SignatureFooter { get; init; }

	/// <summary>
	///     Признак того, что подпись добавляется к постам (опционально)
	/// </summary>
	public bool? SignatureEnabled { get; init; }
}
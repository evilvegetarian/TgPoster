using System.ComponentModel.DataAnnotations;

namespace TgPoster.API.Models;

/// <summary>
///     Создание настроек комментирующего репоста.
/// </summary>
public sealed class CreateCommentRepostRequest
{
	/// <summary>
	///     ID расписания (наш канал-источник).
	/// </summary>
	[Required(ErrorMessage = "Необходимо указать ScheduleId")]
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     ID Telegram сессии для мониторинга и отправки.
	/// </summary>
	[Required(ErrorMessage = "Необходимо указать TelegramSessionId")]
	public required Guid TelegramSessionId { get; set; }

	/// <summary>
	///     Username или ссылка на отслеживаемый канал (@channel или t.me/channel).
	/// </summary>
	[Required(ErrorMessage = "Необходимо указать канал для мониторинга")]
	[StringLength(256, ErrorMessage = "Идентификатор канала не может быть длиннее 256 символов")]
	public required string WatchedChannel { get; set; }
}

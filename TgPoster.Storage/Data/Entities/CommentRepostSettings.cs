using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Настройки комментирующего репоста.
///     Мониторит внешний канал и при новом посте отправляет в комментарии
///     последнюю запись из нашего канала.
/// </summary>
public sealed class CommentRepostSettings : BaseEntity
{
	/// <summary>
	///     Username или ссылка отслеживаемого внешнего канала.
	/// </summary>
	public required string WatchedChannel { get; set; }

	/// <summary>
	///     ID отслеживаемого канала в Telegram.
	/// </summary>
	public required long WatchedChannelId { get; set; }

	/// <summary>
	///     Access hash отслеживаемого канала.
	/// </summary>
	public long? WatchedChannelAccessHash { get; set; }

	/// <summary>
	///     ID discussion group канала (для отправки комментариев).
	/// </summary>
	public required long DiscussionGroupId { get; set; }

	/// <summary>
	///     Access hash discussion group.
	/// </summary>
	public long? DiscussionGroupAccessHash { get; set; }

	/// <summary>
	///     Telegram сессия для мониторинга и отправки.
	/// </summary>
	public required Guid TelegramSessionId { get; set; }

	/// <summary>
	///     Расписание-источник (Schedule.ChannelId = наш канал).
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     Активны ли настройки.
	/// </summary>
	public bool IsActive { get; set; } = true;

	/// <summary>
	///     ID последнего обработанного поста во внешнем канале.
	/// </summary>
	public int? LastProcessedPostId { get; set; }

	/// <summary>
	///     Дата последней проверки.
	/// </summary>
	public DateTime? LastCheckDate { get; set; }

	#region Navigation Properties

	/// <summary>
	///     Расписание-источник.
	/// </summary>
	public Schedule Schedule { get; set; } = null!;

	/// <summary>
	///     Telegram сессия.
	/// </summary>
	public TelegramSession TelegramSession { get; set; } = null!;

	/// <summary>
	///     Журнал отправленных комментариев.
	/// </summary>
	public ICollection<CommentRepostLog> CommentLogs { get; set; } = [];

	#endregion
}

namespace TgPoster.Storage.Data.Entities;

/// <summary>
///     Настройки репостинга сообщений из канала в другие каналы/чаты.
/// </summary>
public sealed class RepostSettings : BaseEntity
{
	/// <summary>
	///     Расписание, к которому привязаны настройки репоста.
	///     Из Schedule.ChannelId берется канал-источник для репоста.
	/// </summary>
	public required Guid ScheduleId { get; set; }

	/// <summary>
	///     Telegram сессия для выполнения репостов через WTelegram.
	/// </summary>
	public required Guid TelegramSessionId { get; set; }

	/// <summary>
	///     Активны ли настройки репоста.
	/// </summary>
	public bool IsActive { get; set; } = true;

	#region Navigation Properties

	/// <summary>
	///     Расписание для репоста.
	/// </summary>
	public Schedule Schedule { get; set; } = null!;

	/// <summary>
	///     Telegram сессия для репоста.
	/// </summary>
	public TelegramSession TelegramSession { get; set; } = null!;

	/// <summary>
	///     Целевые каналы для репоста.
	/// </summary>
	public ICollection<RepostDestination> Destinations { get; set; } = [];

	#endregion
}

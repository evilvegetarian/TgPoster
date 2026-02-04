namespace TgPoster.API.Domain.UseCases.CommentRepost.GetCommentRepost;

/// <summary>
///     Подробная информация о настройках комментирующего репоста.
/// </summary>
public sealed record GetCommentRepostResponse
{
	/// <summary>
	///     Id настроек.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	///     Отслеживаемый канал.
	/// </summary>
	public required string WatchedChannel { get; init; }

	/// <summary>
	///     ID отслеживаемого канала в Telegram.
	/// </summary>
	public required long WatchedChannelId { get; init; }

	/// <summary>
	///     Id расписания.
	/// </summary>
	public required Guid ScheduleId { get; init; }

	/// <summary>
	///     Название расписания (наш канал).
	/// </summary>
	public required string ScheduleName { get; init; }

	/// <summary>
	///     Id телеграм сессии.
	/// </summary>
	public required Guid TelegramSessionId { get; init; }

	/// <summary>
	///     Название телеграм сессии.
	/// </summary>
	public string? TelegramSessionName { get; init; }

	/// <summary>
	///     Активность настроек.
	/// </summary>
	public required bool IsActive { get; init; }

	/// <summary>
	///     ID последнего обработанного поста.
	/// </summary>
	public int? LastProcessedPostId { get; init; }

	/// <summary>
	///     Дата последней проверки.
	/// </summary>
	public DateTime? LastCheckDate { get; init; }

	/// <summary>
	///     Дата создания.
	/// </summary>
	public required DateTimeOffset Created { get; init; }
}

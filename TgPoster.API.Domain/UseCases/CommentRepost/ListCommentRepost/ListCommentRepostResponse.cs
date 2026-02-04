namespace TgPoster.API.Domain.UseCases.CommentRepost.ListCommentRepost;

/// <summary>
///     Элемент списка настроек комментирующего репоста.
/// </summary>
public sealed record CommentRepostItemDto
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
	///     Id расписания.
	/// </summary>
	public required Guid ScheduleId { get; init; }

	/// <summary>
	///     Название расписания (наш канал).
	/// </summary>
	public required string ScheduleName { get; init; }

	/// <summary>
	///     Активность настроек.
	/// </summary>
	public required bool IsActive { get; init; }

	/// <summary>
	///     Дата последней проверки.
	/// </summary>
	public DateTime? LastCheckDate { get; init; }
}

/// <summary>
///     Ответ со списком настроек комментирующего репоста.
/// </summary>
public sealed record ListCommentRepostResponse
{
	public required List<CommentRepostItemDto> Items { get; init; }
}

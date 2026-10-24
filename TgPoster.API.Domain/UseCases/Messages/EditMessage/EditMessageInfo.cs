namespace TgPoster.API.Domain.UseCases.Messages.EditMessage;

/// <summary>
/// Текущее состояние поста для проверки правки
/// </summary>
public sealed record EditMessageInfo
{
	public required Guid ScheduleId { get; init; }

	/// <summary>
	/// Канал расписания, в котором опубликован пост
	/// </summary>
	public required long ChannelId { get; init; }

	public required DateTimeOffset TimePosting { get; init; }

	/// <summary>
	/// Пост уже отправлен в канал
	/// </summary>
	public required bool IsPublished { get; init; }

	public string? Text { get; init; }

	/// <summary>
	/// Подпись расписания, null — подпись выключена
	/// </summary>
	public string? Signature { get; init; }

	/// <summary>
	/// Файлы верхнего уровня в порядке поста
	/// </summary>
	public required List<Guid> FileIds { get; init; }

	public int[]? TelegramMessageIds { get; init; }

	public int? TelegramTextMessageId { get; init; }
}

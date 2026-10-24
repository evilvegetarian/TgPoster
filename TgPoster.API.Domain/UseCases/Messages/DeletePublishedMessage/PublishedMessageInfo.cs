namespace TgPoster.API.Domain.UseCases.Messages.DeletePublishedMessage;

/// <summary>
/// Данные поста для удаления из канала
/// </summary>
public sealed record PublishedMessageInfo
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

	public int[]? TelegramMessageIds { get; init; }
}

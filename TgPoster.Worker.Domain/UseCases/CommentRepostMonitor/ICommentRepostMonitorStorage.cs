namespace TgPoster.Worker.Domain.UseCases.CommentRepostMonitor;

public interface ICommentRepostMonitorStorage
{
	/// <summary>
	///     Получает все активные настройки комментирующего репоста для проверки.
	/// </summary>
	Task<List<CommentRepostSettingDto>> GetActiveSettingsAsync(CancellationToken ct);

	/// <summary>
	///     Обновляет ID последнего обработанного поста и дату проверки.
	/// </summary>
	Task UpdateLastProcessedAsync(Guid settingsId, int lastPostId, CancellationToken ct);
}

public sealed record CommentRepostSettingDto
{
	public required Guid Id { get; init; }
	public required long WatchedChannelId { get; init; }
	public long? WatchedChannelAccessHash { get; init; }
	public required long DiscussionGroupId { get; init; }
	public long? DiscussionGroupAccessHash { get; init; }
	public required Guid TelegramSessionId { get; init; }
	public required long SourceChannelId { get; init; }
	public int? LastProcessedPostId { get; init; }
}

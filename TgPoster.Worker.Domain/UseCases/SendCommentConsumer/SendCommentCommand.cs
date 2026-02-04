namespace TgPoster.Worker.Domain.UseCases.SendCommentConsumer;

public sealed record SendCommentCommand
{
	public required Guid CommentRepostSettingsId { get; init; }
	public required int OriginalPostId { get; init; }
	public required long WatchedChannelId { get; init; }
	public long? WatchedChannelAccessHash { get; init; }
	public required long DiscussionGroupId { get; init; }
	public long? DiscussionGroupAccessHash { get; init; }
	public required long SourceChannelId { get; init; }
	public required Guid TelegramSessionId { get; init; }
}

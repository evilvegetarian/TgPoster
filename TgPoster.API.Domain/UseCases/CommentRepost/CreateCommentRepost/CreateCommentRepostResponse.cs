namespace TgPoster.API.Domain.UseCases.CommentRepost.CreateCommentRepost;

public sealed record CreateCommentRepostResponse
{
	public required Guid Id { get; init; }
}

namespace TgPoster.API.Domain.UseCases.CommentRepost.GetCommentRepost;

public interface IGetCommentRepostStorage
{
	Task<GetCommentRepostResponse?> GetAsync(Guid id, Guid userId, CancellationToken ct);
}

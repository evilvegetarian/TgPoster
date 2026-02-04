namespace TgPoster.API.Domain.UseCases.CommentRepost.ListCommentRepost;

public interface IListCommentRepostStorage
{
	Task<List<CommentRepostItemDto>> GetListAsync(Guid userId, CancellationToken ct);
}

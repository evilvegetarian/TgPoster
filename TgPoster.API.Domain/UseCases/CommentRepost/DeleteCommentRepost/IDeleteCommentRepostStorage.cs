namespace TgPoster.API.Domain.UseCases.CommentRepost.DeleteCommentRepost;

public interface IDeleteCommentRepostStorage
{
	/// <summary>
	///     Проверяет существуют ли настройки и принадлежат ли они пользователю.
	/// </summary>
	Task<bool> ExistsAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Удаляет настройки и все связанные логи.
	/// </summary>
	Task DeleteAsync(Guid id, CancellationToken ct);
}

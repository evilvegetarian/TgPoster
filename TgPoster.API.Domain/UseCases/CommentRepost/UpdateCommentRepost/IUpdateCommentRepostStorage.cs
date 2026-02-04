namespace TgPoster.API.Domain.UseCases.CommentRepost.UpdateCommentRepost;

public interface IUpdateCommentRepostStorage
{
	/// <summary>
	///     Проверяет существуют ли настройки и принадлежат ли они пользователю.
	/// </summary>
	Task<bool> ExistsAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Обновляет статус активности.
	/// </summary>
	Task UpdateAsync(Guid id, bool isActive, CancellationToken ct);
}

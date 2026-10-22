namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestination;

public interface IDeleteRepostDestinationStorage
{
	/// <summary>
	/// Проверяет, что целевой канал существует и его настройки репоста принадлежат пользователю
	/// </summary>
	/// <param name="id"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> DestinationExistsAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Удаляет целевой канал.
	/// </summary>
	Task DeleteDestinationAsync(Guid id, CancellationToken ct);
}
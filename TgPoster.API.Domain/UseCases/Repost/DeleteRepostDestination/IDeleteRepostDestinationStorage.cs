namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestination;

public interface IDeleteRepostDestinationStorage
{
	/// <summary>
	///     Проверяет существует ли destination с указанным ID.
	/// </summary>
	Task<bool> DestinationExistsAsync(Guid id, CancellationToken ct);

	/// <summary>
	///     Удаляет целевой канал.
	/// </summary>
	Task DeleteDestinationAsync(Guid id, CancellationToken ct);
}

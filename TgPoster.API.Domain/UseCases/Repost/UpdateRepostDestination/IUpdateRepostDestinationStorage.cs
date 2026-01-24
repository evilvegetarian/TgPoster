namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

public interface IUpdateRepostDestinationStorage
{
	/// <summary>
	///     Проверяет существует ли destination с указанным ID.
	/// </summary>
	Task<bool> DestinationExistsAsync(Guid id, CancellationToken ct);

	/// <summary>
	///     Обновляет статус активности целевого канала.
	/// </summary>
	Task UpdateDestinationAsync(Guid id, bool isActive, CancellationToken ct);
}

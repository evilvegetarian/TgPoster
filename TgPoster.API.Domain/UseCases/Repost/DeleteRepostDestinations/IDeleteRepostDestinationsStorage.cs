namespace TgPoster.API.Domain.UseCases.Repost.DeleteRepostDestinations;

public interface IDeleteRepostDestinationsStorage
{
	/// <summary>
	/// Проверяет, что настройки репоста существуют и принадлежат пользователю
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> SettingsExistsAsync(Guid settingsId, Guid userId, CancellationToken ct);

	/// <summary>
	/// Возвращает те из указанных каналов, которые относятся к этим настройкам репоста
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="destinationIds"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<Guid>> GetExistingIdsAsync(Guid settingsId, List<Guid> destinationIds, CancellationToken ct);

	/// <summary>
	/// Удаляет целевые каналы
	/// </summary>
	/// <param name="destinationIds"></param>
	/// <param name="ct"></param>
	Task DeleteAsync(List<Guid> destinationIds, CancellationToken ct);
}

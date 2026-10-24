namespace TgPoster.API.Domain.UseCases.Repost.PurgeRepostDestinations;

public interface IPurgeRepostDestinationsStorage
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
	/// Возвращает те из указанных каналов настроек, которые ещё не удалены насовсем, включая удалённые из списка
	/// </summary>
	/// <param name="settingsId"></param>
	/// <param name="destinationIds"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<Guid>> GetPurgeableIdsAsync(Guid settingsId, List<Guid> destinationIds, CancellationToken ct);

	/// <summary>
	/// Удаляет каналы насовсем: выключает, удаляет из списка, если ещё не удалены, и прячет из списка удалённых
	/// </summary>
	/// <param name="destinationIds"></param>
	/// <param name="ct"></param>
	Task PurgeAsync(List<Guid> destinationIds, CancellationToken ct);
}

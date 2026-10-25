namespace TgPoster.API.Domain.UseCases.Discover.UpdateDiscoverSettings;

public interface IUpdateDiscoverSettingsStorage
{
	/// <summary>
	/// ID всех Telegram-сессий пользователя
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<Guid>> GetUserSessionIdsAsync(Guid userId, CancellationToken ct);

	/// <summary>
	/// Создать или перезаписать настройки поиска каналов и одним сохранением переназначить сессии пользователя:
	/// выбранным добавить назначение Discover, остальным его сессиям — снять
	/// </summary>
	/// <param name="settings"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	Task SaveDiscoverSettingsAsync(UpdateDiscoverSettingsCommand settings, Guid userId, CancellationToken ct);
}

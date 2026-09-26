namespace TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;

public interface IUpdateClassifierSettingsStorage
{
	/// <summary>
	///     ID всех Telegram-сессий пользователя
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<Guid>> GetUserSessionIdsAsync(Guid userId, CancellationToken ct);

	/// <summary>
	///     Создать или перезаписать настройки классификатора и одним сохранением переназначить сессии пользователя:
	///     выбранным добавить назначение Classification, остальным его сессиям — снять
	/// </summary>
	/// <param name="settings"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	Task SaveClassifierSettingsAsync(UpdateClassifierSettingsCommand settings, Guid userId, CancellationToken ct);
}

namespace TgPoster.API.Domain.UseCases.Discover.UpdateClassifierSettings;

public interface IUpdateClassifierSettingsStorage
{
	/// <summary>
	///     Какая Telegram-сессия сейчас сохранена в настройках классификатора
	/// </summary>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Guid?> GetTelegramSessionIdAsync(CancellationToken ct);

	/// <summary>
	///     Принадлежит ли Telegram-сессия пользователю
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="sessionId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> TelegramSessionBelongsToUserAsync(Guid userId, Guid sessionId, CancellationToken ct);

	/// <summary>
	///     Создать или перезаписать настройки классификатора
	/// </summary>
	/// <param name="settings"></param>
	/// <param name="ct"></param>
	Task SaveClassifierSettingsAsync(UpdateClassifierSettingsCommand settings, CancellationToken ct);
}

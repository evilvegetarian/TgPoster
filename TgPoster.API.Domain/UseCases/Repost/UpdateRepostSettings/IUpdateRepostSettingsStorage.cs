namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostSettings;

public interface IUpdateRepostSettingsStorage
{
	/// <summary>
	///     Проверяет существует ли настройки репоста с указанным ID для текущего пользователя.
	/// </summary>
	Task<bool> SettingsExistsAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Обновляет активность настроек репоста.
	/// </summary>
	Task UpdateSettingsAsync(Guid id, bool isActive, CancellationToken ct);
}

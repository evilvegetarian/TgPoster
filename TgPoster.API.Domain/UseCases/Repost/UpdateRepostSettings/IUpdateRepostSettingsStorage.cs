namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostSettings;

public interface IUpdateRepostSettingsStorage
{
	/// <summary>
	///     Проверяет существует ли настройки репоста с указанным ID для текущего пользователя.
	/// </summary>
	/// <param name="id">Id настроек репоста.</param>
	/// <param name="userId">Id пользователя.</param>
	/// <param name="ct">Токен отмены.</param>
	/// <returns>True, если настройки существуют и принадлежат пользователю.</returns>
	Task<bool> SettingsExistsAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Обновляет активность и общие настройки репоста, копируемые на новые каналы.
	/// </summary>
	/// <param name="id">Id настроек репоста.</param>
	/// <param name="isActive">Активность настроек.</param>
	/// <param name="defaultDelayMinSeconds">Общая минимальная задержка перед репостом (секунды).</param>
	/// <param name="defaultDelayMaxSeconds">Общая максимальная задержка перед репостом (секунды).</param>
	/// <param name="defaultRepostEveryNth">Общая настройка "репостить каждое N-е сообщение".</param>
	/// <param name="defaultSkipProbability">Общая вероятность пропуска репоста (0-100%).</param>
	/// <param name="defaultMaxRepostsPerDay">Общий лимит репостов в день (null = без лимита).</param>
	/// <param name="ct">Токен отмены.</param>
	Task UpdateSettingsAsync(
		Guid id,
		bool isActive,
		int defaultDelayMinSeconds,
		int defaultDelayMaxSeconds,
		int defaultRepostEveryNth,
		int defaultSkipProbability,
		int? defaultMaxRepostsPerDay,
		CancellationToken ct
	);
}

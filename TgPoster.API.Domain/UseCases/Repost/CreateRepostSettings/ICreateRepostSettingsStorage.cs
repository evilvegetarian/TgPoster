namespace TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

public interface ICreateRepostSettingsStorage
{
	/// <summary>
	/// Проверяет, что Schedule существует и принадлежит пользователю
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> ScheduleExistsAsync(Guid scheduleId, Guid userId, CancellationToken ct);

	/// <summary>
	/// Проверяет, что TelegramSession существует, активна и принадлежит пользователю
	/// </summary>
	/// <param name="telegramSessionId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> TelegramSessionExistsAndActiveAsync(Guid telegramSessionId, Guid userId, CancellationToken ct);

	/// <summary>
	///     Создает настройки репоста и целевые каналы. Возвращает ID созданных настроек.
	/// </summary>
	Task<Guid> CreateRepostSettingsAsync(
		Guid scheduleId,
		Guid telegramSessionId,
		List<long> destinations,
		CancellationToken ct
	);
}
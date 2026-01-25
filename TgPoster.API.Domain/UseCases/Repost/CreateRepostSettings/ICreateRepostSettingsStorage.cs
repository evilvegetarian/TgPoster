namespace TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

public interface ICreateRepostSettingsStorage
{
	/// <summary>
	///     Проверяет существует ли Schedule с указанным ID.
	/// </summary>
	Task<bool> ScheduleExistsAsync(Guid scheduleId, CancellationToken ct);

	/// <summary>
	///     Проверяет существует ли активная TelegramSession с указанным ID.
	/// </summary>
	Task<bool> TelegramSessionExistsAndActiveAsync(Guid telegramSessionId, CancellationToken ct);

	/// <summary>
	///     Проверяет существуют ли уже настройки репоста для Schedule.
	/// </summary>
	Task<bool> RepostSettingsExistForScheduleAsync(Guid scheduleId, CancellationToken ct);

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

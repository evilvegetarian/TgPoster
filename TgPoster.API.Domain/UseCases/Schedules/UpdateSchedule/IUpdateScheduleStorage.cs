namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

public interface IUpdateScheduleStorage
{
	/// <summary>
	///     Обновляет расписание пользователя
	/// </summary>
	/// <param name="id">Идентификатор расписания</param>
	/// <param name="userId">Идентификатор владельца расписания</param>
	/// <param name="name">Новое название, null оставляет прежнее</param>
	/// <param name="youTubeAccountId">Идентификатор YouTube аккаунта, перезаписывается всегда</param>
	/// <param name="telegramBotId">Идентификатор Telegram бота, null оставляет прежний</param>
	/// <param name="signatureFooter">Подпись расписания: null оставляет прежнюю, пустая строка очищает</param>
	/// <param name="signatureEnabled">Признак включения подписи, null оставляет прежний</param>
	/// <param name="ct">Токен отмены</param>
	Task UpdateScheduleAsync(
		Guid id,
		Guid userId,
		string? name,
		Guid? youTubeAccountId,
		Guid? telegramBotId,
		string? signatureFooter,
		bool? signatureEnabled,
		CancellationToken ct
	);

	Task<string?> GetApiTokenAsync(Guid telegramBotId, Guid userId, CancellationToken ct);
	Task<string?> GetChannelNameAsync(Guid scheduleId, Guid userId, CancellationToken ct);
}
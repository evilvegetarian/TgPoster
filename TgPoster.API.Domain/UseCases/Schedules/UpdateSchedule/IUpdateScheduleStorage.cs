namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

public interface IUpdateScheduleStorage
{
	Task UpdateScheduleAsync(Guid id, Guid userId, string? name, Guid? youTubeAccountId, Guid? telegramBotId, CancellationToken ct);
	Task<string?> GetApiTokenAsync(Guid telegramBotId, Guid userId, CancellationToken ct);
	Task<string?> GetChannelNameAsync(Guid scheduleId, Guid userId, CancellationToken ct);
}
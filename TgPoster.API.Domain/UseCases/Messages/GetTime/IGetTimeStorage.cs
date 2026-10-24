namespace TgPoster.API.Domain.UseCases.Messages.GetTime;

public interface IGetTimeStorage
{
	Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ct);
	Task<DateTimeOffset> GetTime(Guid scheduleId, CancellationToken ct);
	Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct);
}
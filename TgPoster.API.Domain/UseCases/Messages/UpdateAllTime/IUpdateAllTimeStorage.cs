namespace TgPoster.API.Domain.UseCases.Messages.UpdateAllTime;

public interface IUpdateAllTimeStorage
{
	public Task<bool> ExistAsync(Guid scheduleId, Guid userId, CancellationToken ct);
	Task<List<Guid>> GetMessagesAsync(Guid scheduleId, CancellationToken ct);
	Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct);
	Task UpdateTimeAsync(List<Guid> messages, List<DateTimeOffset> times, CancellationToken ct);
}
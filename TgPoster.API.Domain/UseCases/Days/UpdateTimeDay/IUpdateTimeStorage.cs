namespace TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;

public interface IUpdateTimeStorage
{
    Task<Guid> DayIdAsync(Guid scheduleId, DayOfWeek dayOfWeek, CancellationToken ct);
    Task UpdateTimeDayAsync(Guid id, List<TimeOnly> times, CancellationToken ct);
}
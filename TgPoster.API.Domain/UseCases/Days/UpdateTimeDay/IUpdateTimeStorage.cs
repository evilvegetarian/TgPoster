namespace TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;

public interface IUpdateTimeStorage
{
    Task<bool> DayExistAsync(Guid id, CancellationToken ct);
    Task UpdateTimeDayAsync(Guid id, List<TimeOnly> times, CancellationToken ct);
}
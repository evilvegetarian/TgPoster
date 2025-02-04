namespace TgPoster.Domain.UseCases.Days.UpdateTimeDay;

public interface IUpdateTimeStorage
{
    Task<bool> DayExistAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateTimeDayAsync(Guid id, List<TimeOnly> times, CancellationToken cancellationToken);
}
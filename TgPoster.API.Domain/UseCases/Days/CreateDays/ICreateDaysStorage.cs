namespace TgPoster.API.Domain.UseCases.Days.CreateDays;

public interface ICreateDaysStorage
{
    Task<bool> ScheduleExistAsync(Guid scheduleId, Guid userId, CancellationToken cancellationToken);
    Task CreateDaysAsync(List<CreateDayDto> days, CancellationToken cancellationToken);
    Task<List<DayOfWeek>> GetDayOfWeek(Guid scheduleId, CancellationToken cancellationToken);
}
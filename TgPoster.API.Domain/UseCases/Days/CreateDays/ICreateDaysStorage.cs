namespace TgPoster.API.Domain.UseCases.Days.CreateDays;

public interface ICreateDaysStorage
{
	Task<bool> ScheduleExistAsync(Guid scheduleId, Guid userId, CancellationToken ct);
	Task CreateDaysAsync(List<CreateDayDto> days, CancellationToken ct);
	Task<List<DayOfWeek>> GetDayOfWeekAsync(Guid scheduleId, CancellationToken ct);
}
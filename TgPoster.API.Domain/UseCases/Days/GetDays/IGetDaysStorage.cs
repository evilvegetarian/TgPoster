namespace TgPoster.API.Domain.UseCases.Days.GetDays;

public interface IGetDaysStorage
{
    Task<bool> ScheduleExist(Guid scheduleId, Guid userId, CancellationToken cancellationToken);
    Task<List<GetDaysResponse>> GetDays(Guid scheduleId, CancellationToken cancellationToken);
}
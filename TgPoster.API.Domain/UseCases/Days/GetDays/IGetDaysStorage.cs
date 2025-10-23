namespace TgPoster.API.Domain.UseCases.Days.GetDays;

public interface IGetDaysStorage
{
	Task<bool> ScheduleExistAsync(Guid scheduleId, Guid userId, CancellationToken ct);
	Task<List<GetDaysResponse>> GetDaysAsync(Guid scheduleId, CancellationToken ct);
}
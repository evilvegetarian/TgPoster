namespace TgPoster.API.Domain.UseCases.Schedules.UpdateActiveSchedule;

public interface IUpdateStatusScheduleStorage
{
    Task<bool> ExistSchedule(Guid id, CancellationToken ct);
    Task UpdateStatus(Guid id, CancellationToken ct);
}
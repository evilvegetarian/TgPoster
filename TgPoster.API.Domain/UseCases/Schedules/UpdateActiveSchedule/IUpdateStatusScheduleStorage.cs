namespace TgPoster.API.Domain.UseCases.Schedules.UpdateActiveSchedule;

public interface IUpdateStatusScheduleStorage
{
    Task<bool> ExistSchedule(Guid id, Guid userId, CancellationToken ct);
    Task UpdateStatus(Guid id, CancellationToken ct);
}
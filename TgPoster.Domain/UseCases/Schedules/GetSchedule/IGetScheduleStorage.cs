using TgPoster.Domain.UseCases.Schedules.ListSchedule;

namespace TgPoster.Domain.UseCases.Schedules.GetSchedule;

public interface IGetScheduleStorage
{
    Task<ScheduleResponse?> GetSchedule(Guid id, Guid userId, CancellationToken cancellationToken);
}
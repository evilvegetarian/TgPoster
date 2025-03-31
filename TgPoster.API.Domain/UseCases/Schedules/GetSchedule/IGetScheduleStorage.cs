using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

namespace TgPoster.API.Domain.UseCases.Schedules.GetSchedule;

public interface IGetScheduleStorage
{
    Task<ScheduleResponse?> GetSchedule(Guid id, Guid userId, CancellationToken cancellationToken);
}
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

namespace TgPoster.API.Domain.UseCases.Schedules.GetSchedule;

public interface IGetScheduleStorage
{
	Task<ScheduleResponse?> GetScheduleAsync(Guid id, Guid userId, CancellationToken ct);
}
namespace TgPoster.Domain.UseCases.Schedules.ListSchedule;

public interface IListScheduleStorage
{
    Task<List<ScheduleResponse>> GetListScheduleAsync(Guid userId, CancellationToken cancellationToken);
}
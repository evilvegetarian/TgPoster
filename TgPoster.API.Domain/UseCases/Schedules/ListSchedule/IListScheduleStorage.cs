namespace TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

public interface IListScheduleStorage
{
	Task<List<ScheduleResponse>> GetListScheduleAsync(Guid userId, CancellationToken ct);
}
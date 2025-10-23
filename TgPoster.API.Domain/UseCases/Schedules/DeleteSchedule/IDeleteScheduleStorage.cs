namespace TgPoster.API.Domain.UseCases.Schedules.DeleteSchedule;

public interface IDeleteScheduleStorage
{
	Task DeleteScheduleAsync(Guid id);
	Task<bool> ScheduleExistAsync(Guid id, Guid userId);
}
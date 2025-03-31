namespace TgPoster.API.Domain.UseCases.Schedules.DeleteSchedule;

public interface IDeleteScheduleStorage
{
    Task DeleteSchedule(Guid id);
    Task<bool> ScheduleExist(Guid id, Guid userId);
}
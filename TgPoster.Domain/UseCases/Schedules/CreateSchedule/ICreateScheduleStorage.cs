namespace TgPoster.Domain.UseCases.Schedules.CreateSchedule;

public interface ICreateScheduleStorage
{
    Task<Guid> CreateSchedule(string name, Guid userId);
}
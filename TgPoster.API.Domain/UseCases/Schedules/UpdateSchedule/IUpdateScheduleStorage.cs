namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

public interface IUpdateScheduleStorage
{
	Task UpdateScheduleAsync(Guid id, Guid userId, Guid? youTubeAccountId, CancellationToken ct);
}
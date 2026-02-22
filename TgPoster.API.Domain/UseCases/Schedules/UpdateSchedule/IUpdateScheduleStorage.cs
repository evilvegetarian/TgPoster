namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

public interface IUpdateScheduleStorage
{
	Task UpdateScheduleAsync(Guid id, Guid userId, string? name, Guid? youTubeAccountId, CancellationToken ct);
}
namespace TgPoster.Worker.Domain.UseCases.ScheduleQueueCheck;

/// <summary>
/// Очередь постов расписания: сколько подтверждённых постов ещё уйдёт и когда последний из них,
/// плюс сколько будущих постов ждут подтверждения
/// </summary>
/// <param name="ScheduleId"></param>
/// <param name="PlannedCount"></param>
/// <param name="LastPlannedAt"></param>
/// <param name="AwaitingApprovalCount"></param>
public sealed record ScheduleQueueInfo(
	Guid ScheduleId,
	int PlannedCount,
	DateTimeOffset? LastPlannedAt,
	int AwaitingApprovalCount);

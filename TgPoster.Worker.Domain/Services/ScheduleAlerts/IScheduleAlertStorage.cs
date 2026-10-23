namespace TgPoster.Worker.Domain.Services.ScheduleAlerts;

public interface IScheduleAlertStorage
{
	/// <summary>
	/// Возвращает, кому и от какого бота слать оповещение по расписанию. Null, если расписания нет
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<ScheduleAlertTarget?> GetAlertTargetAsync(Guid scheduleId, CancellationToken ct);
}

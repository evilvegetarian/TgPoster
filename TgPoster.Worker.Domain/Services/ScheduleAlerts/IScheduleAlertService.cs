namespace TgPoster.Worker.Domain.Services.ScheduleAlerts;

/// <summary>
/// Сервис оповещений о проблемах с расписанием. Пишет владельцу в личку от бота расписания
/// </summary>
public interface IScheduleAlertService
{
	/// <summary>
	/// Отправляет оповещение о проблеме с расписанием. Никогда не бросает исключений: сбой оповещения
	/// не должен ломать основной сценарий. По умолчанию троттлинг считается по расписанию, через
	/// <paramref name="throttleKey" /> его можно вести по другому объекту (например, по источнику парсинга)
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="problem"></param>
	/// <param name="details"></param>
	/// <param name="throttleKey"></param>
	/// <param name="ct"></param>
	Task NotifyAsync(
		Guid scheduleId,
		ScheduleProblem problem,
		string details,
		Guid? throttleKey = null,
		CancellationToken ct = default
	);
}

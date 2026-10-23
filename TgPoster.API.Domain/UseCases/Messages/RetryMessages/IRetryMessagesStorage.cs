namespace TgPoster.API.Domain.UseCases.Messages.RetryMessages;

public interface IRetryMessagesStorage
{
	/// <summary>
	/// Посты из расписаний пользователя, чужие и несуществующие не возвращаются
	/// </summary>
	/// <param name="ids"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<RetryMessageInfo>> GetMessagesAsync(List<Guid> ids, Guid userId, CancellationToken ct);

	/// <summary>
	/// Время публикаций расписания по дням недели
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct);

	/// <summary>
	/// Время запланированых и отправляемых постов расписания позже указанного момента
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="from"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DateTimeOffset>> GetOccupiedTimesAsync(Guid scheduleId, DateTimeOffset from, CancellationToken ct);

	/// <summary>
	/// Вернуть посты в очередь на отправку с новым временем публикации
	/// </summary>
	/// <param name="items"></param>
	/// <param name="ct"></param>
	Task RescheduleAsync(IReadOnlyCollection<RetriedMessage> items, CancellationToken ct);
}

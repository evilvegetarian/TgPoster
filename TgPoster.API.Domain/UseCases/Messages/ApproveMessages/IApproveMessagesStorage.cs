namespace TgPoster.API.Domain.UseCases.Messages.ApproveMessages;

public interface IApproveMessagesStorage
{
	/// <summary>
	/// Посты из расписаний пользователя, чужие и несуществующие не возвращаются
	/// </summary>
	/// <param name="ids"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ApproveMessageInfo>> GetMessagesAsync(List<Guid> ids, Guid userId, CancellationToken ct);

	/// <summary>
	/// Время публикаций расписания по дням недели
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(Guid scheduleId, CancellationToken ct);

	/// <summary>
	/// Время запланированных и отправляемых постов расписания позже указанного момента
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="from"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<DateTimeOffset>> GetOccupiedTimesAsync(Guid scheduleId, DateTimeOffset from, CancellationToken ct);

	/// <summary>
	/// Подтвердить посты пользователя и перенести указанные на новое время, чужие посты не меняются
	/// </summary>
	/// <param name="messageIds"></param>
	/// <param name="userId"></param>
	/// <param name="timePostings"></param>
	/// <param name="ct"></param>
	Task ApproveMessage(
		List<Guid> messageIds,
		Guid userId,
		IReadOnlyDictionary<Guid, DateTimeOffset> timePostings,
		CancellationToken ct
	);
}

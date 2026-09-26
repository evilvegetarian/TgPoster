namespace TgPoster.API.Domain.UseCases.CrossPostTargets.ListCrossPostTargets;

/// <summary>
///     Хранилище для получения связок расписания
/// </summary>
public interface IListCrossPostTargetsStorage
{
	/// <summary>
	///     Проверить, что расписание принадлежит пользователю
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> ScheduleExistsAsync(Guid scheduleId, Guid userId, CancellationToken ct);

	/// <summary>
	///     Получить связки расписания
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<CrossPostTargetResponse>> GetAsync(Guid scheduleId, CancellationToken ct);
}

namespace TgPoster.API.Domain.UseCases.CrossPostTargets.DeleteCrossPostTarget;

/// <summary>
///     Хранилище для удаления связки расписания
/// </summary>
public interface IDeleteCrossPostTargetStorage
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
	///     Проверить, что связка есть в этом расписании
	/// </summary>
	/// <param name="id"></param>
	/// <param name="scheduleId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> ExistsAsync(Guid id, Guid scheduleId, CancellationToken ct);

	/// <summary>
	///     Удалить связку
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task DeleteAsync(Guid id, CancellationToken ct);
}

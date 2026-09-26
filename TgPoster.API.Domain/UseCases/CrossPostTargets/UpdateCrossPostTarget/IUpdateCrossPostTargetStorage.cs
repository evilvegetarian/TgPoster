namespace TgPoster.API.Domain.UseCases.CrossPostTargets.UpdateCrossPostTarget;

/// <summary>
///     Хранилище для обновления связки расписания
/// </summary>
public interface IUpdateCrossPostTargetStorage
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
	///     Обновить связку
	/// </summary>
	/// <param name="command"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task UpdateAsync(UpdateCrossPostTargetCommand command, CancellationToken ct);
}

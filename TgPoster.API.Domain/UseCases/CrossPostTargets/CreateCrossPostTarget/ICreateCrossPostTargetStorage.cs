namespace TgPoster.API.Domain.UseCases.CrossPostTargets.CreateCrossPostTarget;

/// <summary>
///     Хранилище для создания связки расписания
/// </summary>
public interface ICreateCrossPostTargetStorage
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
	///     Проверить, что аккаунт принадлежит пользователю
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> SocialAccountExistsAsync(Guid accountId, Guid userId, CancellationToken ct);

	/// <summary>
	///     Проверить, что связка с этим аккаунтом уже есть
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="accountId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<bool> TargetExistsAsync(Guid scheduleId, Guid accountId, CancellationToken ct);

	/// <summary>
	///     Создать связку
	/// </summary>
	/// <param name="command"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<Guid> CreateAsync(CreateCrossPostTargetCommand command, CancellationToken ct);
}

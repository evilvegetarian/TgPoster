namespace TgPoster.API.Domain.UseCases.CrossPostTargets.PreviewCrossPost;

/// <summary>
///     Хранилище для предпросмотра кросс-поста
/// </summary>
public interface IPreviewCrossPostStorage
{
	/// <summary>
	///     Получить расписание пользователя со связками и последним текстом
	/// </summary>
	/// <param name="scheduleId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<CrossPostPreviewContext?> GetContextAsync(Guid scheduleId, Guid userId, CancellationToken ct);

	/// <summary>
	///     Получить аккаунт пользователя с настройками по умолчанию
	/// </summary>
	/// <param name="accountId"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<CrossPostPreviewTargetDto?> GetAccountAsync(Guid accountId, Guid userId, CancellationToken ct);
}

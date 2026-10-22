using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

public interface IUpdateRepostDestinationStorage
{
	/// <summary>
	/// Получает сессию, чат и текущий статус целевого канала, если его настройки репоста принадлежат пользователю
	/// </summary>
	/// <param name="id"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<RepostDestinationInfo?> GetDestinationAsync(Guid id, Guid userId, CancellationToken ct);

	/// <summary>
	///     Обновляет настройки целевого канала.
	/// </summary>
	Task UpdateDestinationAsync(
		Guid id,
		bool isActive,
		ChatStatus chatStatus,
		int delayMinSeconds,
		int delayMaxSeconds,
		int repostEveryNth,
		int skipProbability,
		int? maxRepostsPerDay,
		CancellationToken ct
	);

	/// <summary>
	/// Фиксирует бан аккаунта сессии в чате: автоматика больше не будет пробовать туда пересылать
	/// </summary>
	/// <param name="telegramSessionId"></param>
	/// <param name="chatId"></param>
	/// <param name="error"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task AddAccountBanAsync(Guid telegramSessionId, long chatId, string? error, CancellationToken ct);

	/// <summary>
	/// Снимает зафиксированный бан аккаунта сессии в чате: доступ только что проверен
	/// </summary>
	/// <param name="telegramSessionId"></param>
	/// <param name="chatId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task RemoveAccountBanAsync(Guid telegramSessionId, long chatId, CancellationToken ct);
}

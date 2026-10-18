using Shared.Enums;

namespace TgPoster.API.Domain.UseCases.Repost.UpdateRepostDestination;

public interface IUpdateRepostDestinationStorage
{
	/// <summary>
	/// Получает сессию, чат и текущий статус целевого канала
	/// </summary>
	/// <param name="id"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<RepostDestinationInfo?> GetDestinationAsync(Guid id, CancellationToken ct);

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
}

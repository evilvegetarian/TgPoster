using TgPoster.API.Domain.Models;

namespace TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;

public interface IGetClassificationHistoryStorage
{
	/// <summary>
	///     Получить страницу классифицированных каналов, от самых свежих к старым
	/// </summary>
	/// <param name="query"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<PagedList<ClassificationHistoryItemResponse>> GetClassificationHistoryAsync(
		GetClassificationHistoryQuery query,
		CancellationToken ct
	);
}

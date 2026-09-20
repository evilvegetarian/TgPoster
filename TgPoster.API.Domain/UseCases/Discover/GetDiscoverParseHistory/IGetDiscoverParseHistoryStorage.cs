using TgPoster.API.Domain.Models;

namespace TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;

public interface IGetDiscoverParseHistoryStorage
{
	/// <summary>
	///     Получить страницу каналов, у которых хотя бы раз парсились ссылки, от самых свежих к старым
	/// </summary>
	/// <param name="query"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<PagedList<DiscoverParseHistoryItemResponse>> GetParseHistoryAsync(
		GetDiscoverParseHistoryQuery query,
		CancellationToken ct
	);
}

using TgPoster.API.Domain.Models;

namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;

public interface IListParseChannelRunsStorage
{
	/// <summary>
	/// Получить страницу журнала по источникам пользователя, от свежих проходов к старым
	/// </summary>
	/// <param name="query"></param>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<PagedList<ParseChannelRunResponse>> GetRunsAsync(
		ListParseChannelRunsQuery query,
		Guid userId,
		CancellationToken ct
	);
}

namespace TgPoster.API.Domain.UseCases.Parse.GetParseChannelStats;

/// <summary>
/// Суммы журнала проходов за один день (UTC)
/// </summary>
/// <param name="Date">День</param>
/// <param name="Published">Новых постов отправлено на обработку</param>
/// <param name="Skipped">Постов отсеяно всеми фильтрами</param>
public sealed record ParseChannelRunDailyDto(DateOnly Date, int Published, int Skipped);

public interface IGetParseChannelStatsStorage
{
	/// <summary>
	/// Количество источников пользователя по статусам (только ненулевые)
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ParseChannelSourceStatusCount>> GetSourceStatusCountsAsync(Guid userId, CancellationToken ct);

	/// <summary>
	/// Неудалённые посты из источников пользователя по состоянию
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<ParseChannelPostTotals> GetPostTotalsAsync(Guid userId, CancellationToken ct);

	/// <summary>
	/// Суммы журнала по проходам источников пользователя, начавшимся с указанного момента
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="since"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<ParseChannelRunTotals> GetRunTotalsAsync(Guid userId, DateTimeOffset since, CancellationToken ct);

	/// <summary>
	/// Суммы журнала по дням, начиная с указанного момента (только дни с проходами)
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="since"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ParseChannelRunDailyDto>> GetRunsByDayAsync(Guid userId, DateTimeOffset since, CancellationToken ct);

	/// <summary>
	/// Источники пользователя с наибольшим числом новых постов с указанного момента
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="since"></param>
	/// <param name="limit"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<List<ParseChannelSourceStat>> GetTopSourcesAsync(
		Guid userId,
		DateTimeOffset since,
		int limit,
		CancellationToken ct
	);

	/// <summary>
	/// Когда закончился последний проход любого источника пользователя
	/// </summary>
	/// <param name="userId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	Task<DateTimeOffset?> GetLastRunAtAsync(Guid userId, CancellationToken ct);
}

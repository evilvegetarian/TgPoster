namespace TgPoster.API.Domain.UseCases.Repost.GetRepostLogsSummary;

public interface IGetRepostLogsSummaryStorage
{
	/// <summary>
	///     Считает сводку по журналу репостов пользователя
	/// </summary>
	/// <param name="userId">Идентификатор пользователя</param>
	/// <param name="query">Фильтры выборки</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Количество записей по статусам и разбивка по причинам</returns>
	Task<RepostLogsSummaryResponse> GetSummaryAsync(
		Guid userId,
		GetRepostLogsSummaryQuery query,
		CancellationToken ct
	);
}

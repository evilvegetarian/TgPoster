using TgPoster.API.Domain.Models;

namespace TgPoster.API.Domain.UseCases.Repost.ListRepostLogs;

public interface IListRepostLogsStorage
{
	/// <summary>
	///     Возвращает страницу журнала репостов пользователя
	/// </summary>
	/// <param name="userId">Идентификатор пользователя</param>
	/// <param name="query">Фильтры и параметры пагинации</param>
	/// <param name="ct">Токен отмены</param>
	/// <returns>Страница записей журнала с общим количеством</returns>
	Task<PagedList<RepostLogDto>> GetRepostLogsAsync(Guid userId, ListRepostLogsQuery query, CancellationToken ct);
}

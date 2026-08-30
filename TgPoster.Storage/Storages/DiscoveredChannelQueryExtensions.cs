using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

/// <summary>
///     Общие фильтрация и сортировка обнаруженных каналов: список на странице Discover
///     и отбор каналов в задание на добавление должны работать одинаково
/// </summary>
internal static class DiscoveredChannelQueryExtensions
{
	/// <summary>
	///     Применить фильтры Discover к запросу
	/// </summary>
	/// <param name="query">Исходный запрос обнаруженных каналов</param>
	/// <param name="category">Тематика (null — без фильтра)</param>
	/// <param name="peerType">Тип: "channel" или "chat" (null — без фильтра)</param>
	/// <param name="search">Подстрока названия или username (null — без поиска)</param>
	/// <param name="minParticipants">Минимальное число подписчиков включительно</param>
	/// <param name="maxParticipants">Максимальное число подписчиков включительно</param>
	/// <returns>Запрос с наложенными фильтрами</returns>
	public static IQueryable<DiscoveredChannel> ApplyDiscoverFilter(
		this IQueryable<DiscoveredChannel> query,
		string? category,
		string? peerType,
		string? search,
		int? minParticipants,
		int? maxParticipants
	) =>
		query
			.Where(x => category == null || x.Category == category)
			.Where(x => peerType == null || x.PeerType == peerType)
			.Where(x => minParticipants == null
			            || (x.ParticipantsCount != null && x.ParticipantsCount >= minParticipants))
			.Where(x => maxParticipants == null
			            || (x.ParticipantsCount != null && x.ParticipantsCount <= maxParticipants))
			.Where(x => search == null
			            || (x.Title != null && x.Title.Contains(search))
			            || (x.Username != null && x.Username.Contains(search)));

	/// <summary>
	///     Применить сортировку Discover к запросу
	/// </summary>
	/// <param name="query">Запрос обнаруженных каналов</param>
	/// <param name="sortBy">Поле сортировки</param>
	/// <param name="sortDirection">Направление сортировки</param>
	/// <returns>Отсортированный запрос</returns>
	public static IQueryable<DiscoveredChannel> ApplyDiscoverSort(
		this IQueryable<DiscoveredChannel> query,
		DiscoverSortBy sortBy,
		SortDirection sortDirection
	) =>
		sortBy switch
		{
			DiscoverSortBy.DiscoveredAt => sortDirection == SortDirection.Asc
				? query.OrderBy(x => x.LastDiscoveredAt)
				: query.OrderByDescending(x => x.LastDiscoveredAt),
			DiscoverSortBy.Title => sortDirection == SortDirection.Asc
				? query.OrderBy(x => x.Title)
				: query.OrderByDescending(x => x.Title),
			_ => sortDirection == SortDirection.Asc
				? query.OrderBy(x => x.ParticipantsCount)
				: query.OrderByDescending(x => x.ParticipantsCount)
		};
}

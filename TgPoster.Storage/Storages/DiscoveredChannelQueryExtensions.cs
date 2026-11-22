using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.Storage.Data.Entities;

namespace TgPoster.Storage.Storages;

/// <summary>
///     Общие фильтрация и сортировка обнаруженных каналов: список на странице Discover
///     и отбор каналов в задание на добавление должны работать одинаково
/// </summary>
internal static class DiscoveredChannelQueryExtensions
{
	/// <summary>Отобрать каналы по языку публикаций с учётом регистра старых записей</summary>
	/// <param name="query">Исходный запрос</param>
	/// <param name="language">ISO-код языка или пустое значение для всех языков</param>
	/// <returns>Запрос с фильтром по языку</returns>
	public static IQueryable<DiscoveredChannel> FilterByLanguage(this IQueryable<DiscoveredChannel> query, string? language)
	{
		if (string.IsNullOrWhiteSpace(language))
		{
			return query;
		}

		var code = language.Trim().ToLowerInvariant();
		return query.Where(x => x.Language != null && x.Language.Trim().ToLower() == code);
	}

	/// <summary>
	///     Применить фильтры Discover к запросу
	/// </summary>
	/// <param name="query">Исходный запрос обнаруженных каналов</param>
	/// <param name="category">Тематика (null — без фильтра)</param>
	/// <param name="peerType">Тип: "channel" или "chat" (null — без фильтра)</param>
	/// <param name="search">Подстрока названия или username (null — без поиска)</param>
	/// <param name="minParticipants">Минимальное число подписчиков включительно</param>
	/// <param name="maxParticipants">Максимальное число подписчиков включительно</param>
	/// <param name="language">ISO-код языка публикаций</param>
	/// <param name="sourceId"></param>
	/// <returns>Запрос с наложенными фильтрами</returns>
	public static IQueryable<DiscoveredChannel> ApplyDiscoverFilter(
		this IQueryable<DiscoveredChannel> query,
		string? category,
		string? peerType,
		string? search,
		int? minParticipants,
		int? maxParticipants,
		string? language,
		Guid? sourceId
	) =>
		query
			.FilterByLanguage(language)
			.Where(x => sourceId == null || x.DiscoveredFromChannelId == sourceId)
			.Where(x => category == null || x.Category == category)
			.Where(x => peerType == null || x.PeerType == peerType)
			.Where(x => minParticipants == null
			            || (x.ParticipantsCount != null && x.ParticipantsCount >= minParticipants))
			.Where(x => maxParticipants == null
			            || (x.ParticipantsCount != null && x.ParticipantsCount <= maxParticipants))
			.Where(x => search == null
			            || (x.Title != null && x.Title.ToLower().Contains(search.ToLower()))
			            || (x.Username != null && x.Username.ToLower().Contains(search.ToLower())));

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

using TgPoster.API.Domain.UseCases.Discover.ListDiscover;

namespace TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;

/// <summary>
///     Фильтр Discover, по которому каналы отбираются в задание вместо явного списка
/// </summary>
/// <param name="Category">Фильтр по тематике (null — без фильтра)</param>
/// <param name="Search">Поиск по названию или username (null — без поиска)</param>
/// <param name="PeerType">Тип: "channel" или "chat" (null — без фильтра)</param>
/// <param name="MinParticipants">Минимальное число подписчиков включительно</param>
/// <param name="MaxParticipants">Максимальное число подписчиков включительно</param>
/// <param name="SortBy">Поле сортировки: задаёт, какие каналы попадут в задание при упирании в лимит</param>
/// <param name="SortDirection">Направление сортировки</param>
public sealed record DiscoverImportFilter(
	string? Category,
	string? Search,
	string? PeerType,
	int? MinParticipants,
	int? MaxParticipants,
	DiscoverSortBy SortBy,
	SortDirection SortDirection);

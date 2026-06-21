using TgPoster.API.Domain.UseCases.Discover.ListDiscover;
using TgPoster.API.Models;
using DiscoverSortBy = TgPoster.API.Domain.UseCases.Discover.ListDiscover.DiscoverSortBy;
using SortDirection = TgPoster.API.Domain.UseCases.Discover.ListDiscover.SortDirection;

namespace TgPoster.API.Mapper;

internal static class ListDiscoverRequestMapper
{
	public static ListDiscoverQuery ToDomain(this ListDiscoverRequest request) =>
		new(
			request.PageNumber,
			request.PageSize,
			request.Category,
			request.Search,
			request.PeerType,
			request.MinParticipants,
			request.MaxParticipants,
			(DiscoverSortBy)request.SortBy,
			(SortDirection)request.SortDirection);
}
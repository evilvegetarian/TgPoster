using TgPoster.API.Domain.UseCases.Repost.AddDestinationsFromDiscover;
using TgPoster.API.Models;
using DiscoverSortBy = TgPoster.API.Domain.UseCases.Discover.ListDiscover.DiscoverSortBy;
using SortDirection = TgPoster.API.Domain.UseCases.Discover.ListDiscover.SortDirection;

namespace TgPoster.API.Mapper;

internal static class AddDestinationsFromDiscoverFilterRequestMapper
{
	public static AddDestinationsFromDiscoverCommand ToDomain(
		this AddDestinationsFromDiscoverFilterRequest request,
		Guid settingsId
	) =>
		new(
			settingsId,
			[],
			request.AutoJoin,
			new DiscoverImportFilter(
				request.Category,
				request.Search,
				request.PeerType,
				request.MinParticipants,
				request.MaxParticipants,
				(DiscoverSortBy)request.SortBy,
				(SortDirection)request.SortDirection));
}

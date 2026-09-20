using TgPoster.API.Domain.UseCases.Discover.GetDiscoverParseHistory;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

internal static class ListDiscoverParseHistoryRequestMapper
{
	public static GetDiscoverParseHistoryQuery ToDomain(this ListDiscoverParseHistoryRequest request) =>
		new(
			request.PageNumber,
			request.PageSize,
			request.Search,
			request.From,
			request.To);
}

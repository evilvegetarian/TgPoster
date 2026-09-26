using TgPoster.API.Domain.UseCases.Discover.GetClassificationHistory;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

internal static class ListClassificationHistoryRequestMapper
{
	public static GetClassificationHistoryQuery ToDomain(this ListClassificationHistoryRequest request) =>
		new(
			request.PageNumber,
			request.PageSize,
			request.Search,
			request.Category,
			request.Confidence,
			request.From,
			request.To);
}

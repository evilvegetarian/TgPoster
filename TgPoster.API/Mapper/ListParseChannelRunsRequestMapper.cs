using TgPoster.API.Domain.UseCases.Parse.ListParseChannelRuns;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

internal static class ListParseChannelRunsRequestMapper
{
	public static ListParseChannelRunsQuery ToDomain(this ListParseChannelRunsRequest request) =>
		new(
			request.PageNumber,
			request.PageSize,
			request.ParseChannelId,
			request.Status,
			request.From,
			request.To);
}

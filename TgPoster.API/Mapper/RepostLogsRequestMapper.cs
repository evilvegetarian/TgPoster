using TgPoster.API.Domain.UseCases.Repost.GetRepostLogsSummary;
using TgPoster.API.Domain.UseCases.Repost.ListRepostLogs;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

internal static class RepostLogsRequestMapper
{
	public static ListRepostLogsQuery ToDomain(this ListRepostLogsRequest request) =>
		new(
			request.RepostSettingsId,
			request.DestinationId,
			request.MessageId,
			request.Status,
			request.From,
			request.To,
			request.PageNumber,
			request.PageSize);

	public static GetRepostLogsSummaryQuery ToDomain(this RepostLogsSummaryRequest request) =>
		new(
			request.RepostSettingsId,
			request.DestinationId,
			request.MessageId,
			request.From,
			request.To);
}

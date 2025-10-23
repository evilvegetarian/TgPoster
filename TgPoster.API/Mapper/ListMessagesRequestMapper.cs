using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Models;
using MessageSortBy = TgPoster.API.Domain.UseCases.Messages.ListMessage.MessageSortBy;
using MessageStatus = TgPoster.API.Domain.UseCases.Messages.ListMessage.MessageStatus;
using SortDirection = TgPoster.API.Domain.UseCases.Messages.ListMessage.SortDirection;

namespace TgPoster.API.Mapper;

public static class ListMessagesRequestMapper
{
	public static ListMessageQuery ToDomain(this ListMessagesRequest request)
	{
		var status = (MessageStatus)request.Status;
		var sortBy = (MessageSortBy)request.SortBy;
		var sortDirection = (SortDirection)request.SortDirection;

		return new ListMessageQuery(
			request.ScheduleId,
			request.PageNumber,
			request.PageSize,
			sortBy,
			sortDirection,
			request.SearchText,
			status
		);
	}
}
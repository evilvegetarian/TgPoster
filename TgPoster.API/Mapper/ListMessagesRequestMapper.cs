using TgPoster.API.Domain.UseCases.Messages.ListMessage;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

public static class ListMessagesRequestMapper
{
    public static ListMessageQuery ToDomain(this ListMessagesRequest request)
    {
        var status = (Domain.UseCases.Messages.ListMessage.MessageStatus)request.Status;
        var sortBy = (Domain.UseCases.Messages.ListMessage.MessageSortBy)request.SortBy;
        var sortDirection = (Domain.UseCases.Messages.ListMessage.SortDirection)request.SortDirection;

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
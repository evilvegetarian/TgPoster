using TgPoster.API.Domain.UseCases.Parse.ListChannel;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Mapper;

public static class ChannelParsingParametersMapper
{
    public static ParseChannelsResponse ToDomain(this ChannelParsingParameters entity)
    {
        return new ParseChannelsResponse
        {
            Id = entity.Id,
            Status = entity.Status.GetStatus(),
            AvoidWords = entity.AvoidWords,
            DateFrom = entity.DateFrom,
            DateTo = entity.DateTo,
            DeleteMedia = entity.DeleteMedia,
            DeleteText = entity.DeleteText,
            ScheduleId = entity.ScheduleId,
            NeedVerifiedPosts = entity.NeedVerifiedPosts
        };
    }
}
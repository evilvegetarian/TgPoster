using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

public static class ParsingMapping
{
    public static ParseChannelCommand ToCommand(this ParseChannelRequest request)
    {
        return new ParseChannelCommand(
            request.Channel,
            request.AlwaysCheckNewPosts,
            request.ScheduleId,
            request.DeleteText,
            request.DeleteMedia,
            request.AvoidWords,
            request.NeedVerifiedPosts,
            request.DateFrom,
            request.DateTo);
    }
}
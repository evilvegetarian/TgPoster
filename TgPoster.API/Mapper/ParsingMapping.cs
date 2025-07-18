using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

/// <summary>
/// 
/// </summary>
public static class ParsingMapping
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static CreateParseChannelCommand ToCommand(this ParseChannelRequest request)
    {
        return new CreateParseChannelCommand(
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
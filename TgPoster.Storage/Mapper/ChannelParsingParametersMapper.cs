using TgPoster.API.Domain.UseCases.Parse.ListParseChannel;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
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
			NeedVerifiedPosts = entity.NeedVerifiedPosts,
			IsActive = entity.Status.IsActive(),
			Channel = entity.Channel,
			LastParseDate = entity.LastParseDate
		};
	}

	public static ChannelParsingParameters ToEntity(this UpdateParseChannelCommand request)
	{
		return new ChannelParsingParameters
		{
			Id = request.Id,
			Channel = request.Channel,
			ScheduleId = request.ScheduleId,
			AvoidWords = request.AvoidWords,
			DateTo = request.DateTo,
			DateFrom = request.DateFrom,
			DeleteText = request.DeleteText,
			CheckNewPosts = request.AlwaysCheckNewPosts,
			NeedVerifiedPosts = request.NeedVerifiedPosts,
			DeleteMedia = request.DeleteMedia
		};
	}
}
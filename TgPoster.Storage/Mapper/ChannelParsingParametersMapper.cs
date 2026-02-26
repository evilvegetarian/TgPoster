using TgPoster.API.Domain.UseCases.Parse.ListParseChannel;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
using TgPoster.Storage.Data.Entities;
using TgPoster.Storage.Data.Enum;

namespace TgPoster.Storage.Mapper;

public static class ChannelParsingParametersMapper
{
	public static ParseChannelResponse ToDomain(this ChannelParsingSetting entity, int parsedMessagesCount = 0) =>
		new()
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
			LastParseDate = entity.LastParseDate,
			ScheduleName = entity.Schedule.Name,
			TelegramSessionId = entity.TelegramSessionId,
			TotalMessagesCount = entity.TotalMessagesCount,
			ParsedMessagesCount = parsedMessagesCount
		};

	public static ChannelParsingSetting ToEntity(this UpdateParseChannelCommand request) =>
		new()
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
			DeleteMedia = request.DeleteMedia,
			UseAiForPosts = request.UseAiForPosts,
			TelegramSessionId = request.TelegramSessionId
		};
}
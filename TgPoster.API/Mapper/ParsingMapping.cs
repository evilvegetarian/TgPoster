using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

/// <summary>
/// </summary>
public static class ParsingMapping
{
	/// <summary>
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	public static CreateParseChannelCommand ToCommand(this CreateParseChannelRequest request) =>
		new(
			request.Channel,
			request.AlwaysCheckNewPosts,
			request.ScheduleId,
			request.DeleteText,
			request.DeleteMedia,
			request.AvoidWords,
			request.NeedVerifiedPosts,
			request.DateFrom,
			request.DateTo,
			request.UseAiForPosts);

	public static UpdateParseChannelCommand ToCommand(this UpdateParseChannelRequest request, Guid id) =>
		new(
			id,
			request.Channel,
			request.AlwaysCheckNewPosts,
			request.ScheduleId,
			request.DeleteText,
			request.DeleteMedia,
			request.AvoidWords,
			request.NeedVerifiedPosts,
			request.DateFrom,
			request.DateTo,
			request.UseAiForPosts);
}
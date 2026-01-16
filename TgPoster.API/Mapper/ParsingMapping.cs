using TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;
using TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;
using TgPoster.API.Models;

namespace TgPoster.API.Mapper;

/// <summary>
/// Маппер для запросов парсинга каналов
/// </summary>
public static class ParsingMapping
{
	/// <summary>
	/// Преобразовать API модель в команду создания парсинга канала
	/// </summary>
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

	/// <summary>
	/// Преобразовать API модель в команду обновления парсинга канала
	/// </summary>
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
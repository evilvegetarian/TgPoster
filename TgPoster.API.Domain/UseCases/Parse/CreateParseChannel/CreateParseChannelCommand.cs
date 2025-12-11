using MediatR;

namespace TgPoster.API.Domain.UseCases.Parse.CreateParseChannel;

public sealed record CreateParseChannelCommand(
	string Channel,
	bool AlwaysCheckNewPosts,
	Guid ScheduleId,
	bool DeleteText,
	bool DeleteMedia,
	string[] AvoidWords,
	bool NeedVerifiedPosts,
	DateTime? DateFrom,
	DateTime? DateTo,
	bool UseAiForPosts) : IRequest<CreateParseChannelResponse>;
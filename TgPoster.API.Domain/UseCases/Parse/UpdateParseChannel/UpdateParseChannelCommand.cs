using MediatR;

namespace TgPoster.API.Domain.UseCases.Parse.UpdateParseChannel;

public record UpdateParseChannelCommand(
	Guid Id,
	string Channel,
	bool AlwaysCheckNewPosts,
	Guid ScheduleId,
	bool DeleteText,
	bool DeleteMedia,
	string[] AvoidWords,
	bool NeedVerifiedPosts,
	DateTime? DateFrom,
	DateTime? DateTo,
	bool UseAiForPosts,
	Guid TelegramSessionId
) : IRequest;
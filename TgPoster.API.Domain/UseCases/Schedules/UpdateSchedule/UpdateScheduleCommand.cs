using MediatR;

namespace TgPoster.API.Domain.UseCases.Schedules.UpdateSchedule;

public sealed record UpdateScheduleCommand(
	Guid Id,
	string? Name,
	Guid? YouTubeAccountId,
	Guid? TelegramBotId,
	string? SignatureFooter,
	bool? SignatureEnabled
) : IRequest;
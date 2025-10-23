using MediatR;

namespace TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;

public sealed record CreateScheduleCommand(string Name, Guid TelegramBotId, string Channel)
	: IRequest<CreateScheduleResponse>;
using MediatR;

namespace TgPoster.Domain.UseCases.Schedules.CreateSchedule;

public sealed record CreateScheduleCommand(string Name, Guid TelegramBotId) : IRequest<CreateScheduleResponse>;

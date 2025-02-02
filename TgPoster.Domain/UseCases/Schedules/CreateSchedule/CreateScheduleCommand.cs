using MediatR;

namespace TgPoster.Domain.UseCases.Schedules.CreateSchedule;

public sealed record CreateScheduleCommand(string Name) : IRequest<CreateScheduleResponse>;
using MediatR;
using TgPoster.Domain.UseCases.Schedules.ListSchedule;

namespace TgPoster.Domain.UseCases.Schedules.GetSchedule;

public sealed record GetScheduleCommand(Guid Id) : IRequest<ScheduleResponse>;
using MediatR;
using TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

namespace TgPoster.API.Domain.UseCases.Schedules.GetSchedule;

public sealed record GetScheduleCommand(Guid Id) : IRequest<ScheduleResponse>;
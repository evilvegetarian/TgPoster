using MediatR;

namespace TgPoster.Domain.UseCases.Schedules.DeleteSchedule;

public sealed record DeleteScheduleCommand(Guid Id) : IRequest;
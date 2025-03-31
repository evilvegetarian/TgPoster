using MediatR;

namespace TgPoster.API.Domain.UseCases.Schedules.DeleteSchedule;

public sealed record DeleteScheduleCommand(Guid Id) : IRequest;
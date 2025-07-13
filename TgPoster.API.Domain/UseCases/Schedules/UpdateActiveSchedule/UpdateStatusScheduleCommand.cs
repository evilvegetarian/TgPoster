using MediatR;

namespace TgPoster.API.Domain.UseCases.Schedules.UpdateActiveSchedule;

public record UpdateStatusScheduleCommand(Guid Id) : IRequest;
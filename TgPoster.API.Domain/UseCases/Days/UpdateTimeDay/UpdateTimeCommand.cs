using MediatR;

namespace TgPoster.API.Domain.UseCases.Days.UpdateTimeDay;

public sealed record UpdateTimeCommand(Guid ScheduleId, DayOfWeek DayOfWeek, List<TimeOnly> Times) : IRequest;
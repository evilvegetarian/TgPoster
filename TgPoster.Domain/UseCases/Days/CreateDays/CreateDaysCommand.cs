using MediatR;

namespace TgPoster.Domain.UseCases.Days.CreateDays;

public sealed record CreateDaysCommand(
    Guid ScheduleId,
    List<DayOfWeekForm> DayOfWeekForms
) : IRequest;

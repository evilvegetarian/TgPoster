using MediatR;

namespace TgPoster.API.Domain.UseCases.Days.CreateDays;

public sealed record CreateDaysCommand(
	Guid ScheduleId,
	List<DayOfWeekForm> DayOfWeekForms
) : IRequest;
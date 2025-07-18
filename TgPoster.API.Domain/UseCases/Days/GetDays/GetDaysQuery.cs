using MediatR;

namespace TgPoster.API.Domain.UseCases.Days.GetDays;

public sealed record GetDaysQuery(Guid ScheduleId) : IRequest<List<GetDaysResponse>>;
using MediatR;

namespace TgPoster.Domain.UseCases.Days.GetDayOfWeek;

public sealed record DayOfWeekQuery : IRequest<List<DayOfWeekResponse>>;
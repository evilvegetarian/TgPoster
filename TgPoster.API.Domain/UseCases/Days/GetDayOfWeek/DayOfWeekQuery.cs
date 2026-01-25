using MediatR;

namespace TgPoster.API.Domain.UseCases.Days.GetDayOfWeek;

public sealed record DayOfWeekQuery : IRequest<DayOfWeekListResponse>;
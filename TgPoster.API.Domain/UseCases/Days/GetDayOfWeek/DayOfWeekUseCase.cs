using MediatR;

namespace TgPoster.API.Domain.UseCases.Days.GetDayOfWeek;

internal sealed class DayOfWeekUseCase : IRequestHandler<DayOfWeekQuery, List<DayOfWeekResponse>>
{
    public Task<List<DayOfWeekResponse>> Handle(DayOfWeekQuery request, CancellationToken ct)
    {
        return Task.FromResult(Enum.GetValues<DayOfWeek>()
            .Select(x => new DayOfWeekResponse((int)x, x.ToString()))
            .ToList());
    }
}
using MediatR;

namespace TgPoster.Domain.UseCases.Days.GetDayOfWeek;

internal sealed class DayOfWeekUseCase : IRequestHandler<DayOfWeekQuery, List<DayOfWeekResponse>>
{
    public async Task<List<DayOfWeekResponse>> Handle(DayOfWeekQuery request, CancellationToken cancellationToken)
    {
        return Enum.GetValues<DayOfWeek>()
            .Select(x => new DayOfWeekResponse((int)x, x.ToString()))
            .ToList();
        await Task.CompletedTask;
    }
}
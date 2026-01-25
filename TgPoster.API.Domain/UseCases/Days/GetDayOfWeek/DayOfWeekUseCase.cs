using MediatR;

namespace TgPoster.API.Domain.UseCases.Days.GetDayOfWeek;

internal sealed class DayOfWeekUseCase : IRequestHandler<DayOfWeekQuery, DayOfWeekListResponse>
{
	public Task<DayOfWeekListResponse> Handle(DayOfWeekQuery request, CancellationToken ct)
	{
		var items = Enum.GetValues<DayOfWeek>()
			.Select(x => new DayOfWeekResponse((int)x, x.ToString()))
			.ToList();
		return Task.FromResult(new DayOfWeekListResponse { Items = items });
	}
}
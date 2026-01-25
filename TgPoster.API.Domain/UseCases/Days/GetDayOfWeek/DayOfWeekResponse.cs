namespace TgPoster.API.Domain.UseCases.Days.GetDayOfWeek;

public sealed record DayOfWeekResponse(int Id, string Name);

public sealed record DayOfWeekListResponse
{
	public required List<DayOfWeekResponse> Items { get; init; }
}
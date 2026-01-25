namespace TgPoster.API.Domain.UseCases.Days.GetDays;

public sealed record DayResponse
{
	public required Guid Id { get; init; }
	public required Guid ScheduleId { get; init; }
	public required DayOfWeek DayOfWeek { get; init; }
	public ICollection<TimeOnly> TimePostings { get; init; } = [];
}

public sealed record DayListResponse
{
	public required List<DayResponse> Items { get; init; }
}
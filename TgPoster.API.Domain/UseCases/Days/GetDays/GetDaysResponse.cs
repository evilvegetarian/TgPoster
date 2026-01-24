namespace TgPoster.API.Domain.UseCases.Days.GetDays;

public sealed record GetDaysResponse
{
	public required Guid Id { get; init; }
	public required Guid ScheduleId { get; init; }
	public required DayOfWeek DayOfWeek { get; init; }
	public ICollection<TimeOnly> TimePostings { get; init; } = [];
}
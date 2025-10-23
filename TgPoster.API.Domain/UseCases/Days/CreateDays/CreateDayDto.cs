namespace TgPoster.API.Domain.UseCases.Days.CreateDays;

public sealed class CreateDayDto
{
	public required Guid ScheduleId { get; set; }
	public required DayOfWeek DayOfWeek { get; set; }
	public ICollection<TimeOnly> TimePostings { get; set; } = [];
}
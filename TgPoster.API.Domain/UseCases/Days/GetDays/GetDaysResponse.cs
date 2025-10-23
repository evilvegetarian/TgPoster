namespace TgPoster.API.Domain.UseCases.Days.GetDays;

public class GetDaysResponse
{
	public required Guid Id { get; set; }
	public required Guid ScheduleId { get; set; }
	public required DayOfWeek DayOfWeek { get; set; }
	public ICollection<TimeOnly> TimePostings { get; set; } = [];
}
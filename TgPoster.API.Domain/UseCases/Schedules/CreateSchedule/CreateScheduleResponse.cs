namespace TgPoster.API.Domain.UseCases.Schedules.CreateSchedule;

public sealed record CreateScheduleResponse
{
	public required Guid Id { get; init; }
}
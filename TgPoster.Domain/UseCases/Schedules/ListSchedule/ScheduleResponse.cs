namespace TgPoster.Domain.UseCases.Schedules.ListSchedule;

public sealed class ScheduleResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
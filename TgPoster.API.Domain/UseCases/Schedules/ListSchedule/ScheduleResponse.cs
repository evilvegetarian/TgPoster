namespace TgPoster.API.Domain.UseCases.Schedules.ListSchedule;

public sealed class ScheduleResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required string ChannelName { get; init; }
}
namespace TgPoster.Worker.Domain.UseCases.RepostMessageConsumer;

public sealed record RepostMessageCommand
{
	public required Guid MessageId { get; init; }
	public required Guid ScheduleId { get; init; }
	public required Guid RepostSettingsId { get; init; }
}
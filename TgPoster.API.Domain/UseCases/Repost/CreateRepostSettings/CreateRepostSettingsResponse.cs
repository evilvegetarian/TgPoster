namespace TgPoster.API.Domain.UseCases.Repost.CreateRepostSettings;

public sealed record CreateRepostSettingsResponse
{
	public required Guid Id { get; init; }
	public required Guid ScheduleId { get; init; }
	public required Guid TelegramSessionId { get; init; }
	public required List<RepostDestinationDto> Destinations { get; init; }
}

public sealed record RepostDestinationDto
{
	public required Guid Id { get; init; }
	public required string ChatIdentifier { get; init; }
	public required bool IsActive { get; init; }
}

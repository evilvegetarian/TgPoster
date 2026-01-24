namespace TgPoster.API.Domain.UseCases.Repost.GetRepostSettings;

public sealed record GetRepostSettingsResponse
{
	public required Guid Id { get; init; }
	public required Guid ScheduleId { get; init; }
	public required Guid TelegramSessionId { get; init; }
	public required List<GetRepostDestinationDto> Destinations { get; init; }
}

public sealed record GetRepostDestinationDto
{
	public required Guid Id { get; init; }
	public required string ChatIdentifier { get; init; }
	public required bool IsActive { get; init; }
}

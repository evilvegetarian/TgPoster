namespace TgPoster.API.Domain.UseCases.Messages.GenerateAiContent;

public sealed class OpenRouterDto
{
	public required string Token { get; set; }
	public required string Model { get; set; }
	public required Guid ScheduleId { get; set; }
}
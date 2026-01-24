namespace TgPoster.API.Domain.UseCases.Messages.GenerateAiContent;

public sealed record GenerateAiContentResponse
{
	public required string Content { get; init; }
}
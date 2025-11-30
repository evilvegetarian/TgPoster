using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.Messages.GenerateAiContent;

public sealed class GenerateAiContentMessageDto
{
	public required Guid Id { get; set; }
	public string? TextMessage { get; set; }
	public List<FileDto> Files { get; set; } = [];
}
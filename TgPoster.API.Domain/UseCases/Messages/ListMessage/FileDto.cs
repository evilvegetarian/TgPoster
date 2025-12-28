namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed class FileDto
{
	public required Guid Id { get; set; }
	public required string TgFileId { get; set; }
	public required string ContentType { get; set; }
	public List<PreviewDto> Previews { get; set; } = [];
}

public sealed class PreviewDto
{
	public required Guid Id { get; set; }
	public required string TgFileId { get; set; }
}
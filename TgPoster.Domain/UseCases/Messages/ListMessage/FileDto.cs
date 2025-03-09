using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Messages.ListMessage;

public sealed class FileDto
{
    public required Guid Id { get; set; }
    public required string TgFileId { get; set; }
    public required string ContentType { get; set; }
    public List<string> PreviewIds { get; set; } = [];
}
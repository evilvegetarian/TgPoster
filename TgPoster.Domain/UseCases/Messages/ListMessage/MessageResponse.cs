namespace TgPoster.Domain.UseCases.Messages.ListMessage;

public class MessageResponse
{
    public required Guid Id { get; set; }
    public string? TextMessage { get; set; }
    public List<FileResponse> Files { get; set; } = [];
}
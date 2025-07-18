namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed class MessageResponse
{
    public required Guid Id { get; set; }
    public string? TextMessage { get; set; }
    public required DateTimeOffset TimePosting { get; set; }
    public required Guid ScheduleId { get; set; }
    public List<FileResponse> Files { get; set; } = [];
}
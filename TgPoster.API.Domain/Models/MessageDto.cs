namespace TgPoster.API.Domain.Models;

public class MessageDto
{
    public Guid Id { get; set; }
    public string? TextMessage { get; set; }
    public DateTimeOffset? TimePosting { get; set; }
    public Guid ScheduleId { get; set; }
    public List<FileDto> Files { get; set; } = new();
}

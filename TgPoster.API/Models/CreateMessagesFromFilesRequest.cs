namespace TgPoster.API.Models;

public class CreateMessagesFromFilesRequest
{
    public required Guid ScheduleId { get; set; }
    public required List<IFormFile> Files { get; set; }
}
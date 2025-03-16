namespace TgPoster.Domain.UseCases.BackGround.SenderMessageWorker;

public class MessageDto
{
    public Guid Id { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset TimePosting { get; set; }
    public List<FileDto> File { get; set; } = [];
}
namespace TgPoster.Domain.UseCases.BackGround.SenderMessageWorker;

public class FileDto
{
    public required string TgFileId { get; set; }
    public string? Caption { get; set; }
    public required string ContentType { get; set; }
}
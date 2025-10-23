namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public class MediaDto
{
	public required string MimeType { get; set; }
	public required string FileId { get; set; }
	public List<string> PreviewPhotoIds { get; set; } = [];
}
namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public class MediaStreamDto
{
	public MemoryStream? Photo { get; set; }
	public List<MemoryStream> PreviewPhoto { get; set; } = [];
}
namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public class MessageDto
{
	public string? Text { get; set; }
	public Guid ScheduleId { get; set; }
	public bool IsNeedVerified { get; set; }
	public DateTimeOffset TimePosting { get; set; } = DateTimeOffset.MinValue;
	public List<MediaDto> Media { get; set; } = [];
}
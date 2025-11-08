namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public sealed class MessageDto
{
	public required Guid Id { get; set; }
	public required Guid ScheduleId { get; set; }
	public required DateTimeOffset TimePosting { get; set; }
	public string? TextMessage { get; set; }
	public bool IsVerified { get; set; }
	public List<FileDto> Files { get; set; } = [];
	public DateTime? Created { get; set; }
	public bool IsSent { get; set; }
}
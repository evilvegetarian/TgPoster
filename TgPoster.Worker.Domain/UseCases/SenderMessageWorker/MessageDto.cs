namespace TgPoster.Worker.Domain.UseCases.SenderMessageWorker;

public class MessageDto
{
	public Guid Id { get; set; }
	public string? Message { get; set; }
	public DateTimeOffset TimePosting { get; set; }
	public List<FileDto> File { get; set; } = [];

	/// <summary>
	///     Общая подпись расписания, приклеиваемая снизу к посту. Null, если подпись выключена
	/// </summary>
	public string? Signature { get; set; }
}
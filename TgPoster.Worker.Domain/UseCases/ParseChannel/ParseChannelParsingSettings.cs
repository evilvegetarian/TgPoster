namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public class ParseChannelParsingSettings
{
	public required string ChannelName { get; set; }
	public DateTime? FromDate { get; set; }
	public DateTime? ToDate { get; set; }
	public string[] AvoidWords { get; set; } = [];
	public int? LastParsedId { get; set; }
	public bool CheckNewPosts { get; set; }
	public Guid TelegramBotId { get; set; }
	public required Guid TelegramSessionId { get; set; }
}
namespace TgPoster.Worker.Domain.UseCases.ParseChannel;

public class ParametersDto
{
	public required string ChannelName { get; set; }
	public bool IsNeedVerified { get; set; }
	public required string Token { get; set; }
	public long ChatId { get; set; }
	public bool DeleteText { get; set; }
	public bool DeleteMedia { get; set; }
	public DateTime? FromDate { get; set; }
	public DateTime? ToDate { get; set; }
	public string[] AvoidWords { get; set; } = [];
	public int? LastParsedId { get; set; }
	public Guid ScheduleId { get; set; }
	public bool CheckNewPosts { get; set; }
	public required Guid TelegramBotId { get; set; }
	public required bool UseAi { get; set; }
	public string? TokenOpenRouter { get; set; }
	public string? ModelOpenRouter { get; set; }
	public string? Prompt { get; set; }
	public required Guid TelegramSessionId { get; set; }
}
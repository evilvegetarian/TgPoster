namespace TgPoster.Worker.Domain.ConfigModels;

public class TelegramOptions
{
	public required string SecretKey { get; set; }
	public required Guid TelegramSessionId { get; set; }
}
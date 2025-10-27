namespace TgPoster.Worker.Domain.ConfigModels;

public class TelegramSettings
{
	public required string api_id { get; set; }
	public required string api_hash { get; set; }
	public required string phone_number { get; set; }
}
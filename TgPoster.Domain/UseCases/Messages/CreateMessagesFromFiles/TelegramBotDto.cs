namespace TgPoster.Domain.UseCases.Messages.CreateMessagesFromFiles;

public class TelegramBotDto
{
    public required string ApiTelegram { get; set; }
    public required long ChatId { get; set; }
}
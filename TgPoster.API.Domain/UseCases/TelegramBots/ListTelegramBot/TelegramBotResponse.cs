namespace TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;

public sealed class TelegramBotResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}
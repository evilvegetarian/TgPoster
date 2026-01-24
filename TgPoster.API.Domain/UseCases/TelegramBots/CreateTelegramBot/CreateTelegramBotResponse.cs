namespace TgPoster.API.Domain.UseCases.TelegramBots.CreateTelegramBot;

public sealed record CreateTelegramBotResponse
{
	public required Guid Id { get; init; }
}
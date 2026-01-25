namespace TgPoster.API.Domain.UseCases.TelegramBots.ListTelegramBot;

public sealed record TelegramBotResponse
{
	public required Guid Id { get; init; }
	public required string Name { get; init; }
}

public sealed record TelegramBotListResponse
{
	public required List<TelegramBotResponse> Items { get; init; }
}
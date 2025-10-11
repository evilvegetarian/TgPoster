namespace TgPoster.API.Domain.UseCases.TelegramBots.UpdateTelegramBot;

public interface IUpdateTelegramBotStorage
{
    Task UpdateTelegramBotAsync(Guid id, string name, bool isActive, CancellationToken ct);
}
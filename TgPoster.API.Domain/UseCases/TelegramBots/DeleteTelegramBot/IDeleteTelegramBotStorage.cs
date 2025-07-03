namespace TgPoster.API.Domain.UseCases.TelegramBots.DeleteTelegramBot;

public interface IDeleteTelegramBotStorage
{
    Task<bool> ExistsAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken);
    Task DeleteTelegramBotAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken);
}
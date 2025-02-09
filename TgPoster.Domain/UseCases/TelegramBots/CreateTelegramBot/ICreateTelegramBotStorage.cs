namespace TgPoster.Domain.UseCases.TelegramBots.CreateTelegramBot;

public interface ICreateTelegramBotStorage
{
    public Task<Guid> CreateTelegramBotAsync(
        string apiToken,
        long chatId,
        Guid ownerId,
        string name,
        CancellationToken cancellationToken
    );
}
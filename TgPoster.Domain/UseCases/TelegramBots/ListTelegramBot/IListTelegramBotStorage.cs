namespace TgPoster.Domain.UseCases.TelegramBots.ListTelegramBot;

public interface IListTelegramBotStorage
{
    Task<List<TelegramBotResponse>> GetTelegramBotListAsync(Guid userId, CancellationToken cancellationToken);
}
namespace TgPoster.Domain.Services;

public interface IGetTelegramBotStorage
{
    Task<string?> GetApiToken(Guid scheduleId, Guid userId, CancellationToken cancellationToken);
}
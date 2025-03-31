namespace TgPoster.API.Domain.Services;

public interface IGetTelegramBotStorage
{
    Task<string?> GetApiToken(Guid scheduleId, Guid userId, CancellationToken cancellationToken);
}
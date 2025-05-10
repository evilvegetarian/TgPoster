namespace TgPoster.API.Domain.Services;

public interface IGetTelegramBotStorage
{
    Task<string?> GetApiTokenAsync(Guid scheduleId, Guid userId, CancellationToken ct);
}
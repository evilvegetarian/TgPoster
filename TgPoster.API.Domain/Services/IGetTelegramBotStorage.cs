using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;

namespace TgPoster.API.Domain.Services;

public interface IGetTelegramBotStorage
{
	Task<TelegramBotDto?> GetApiTokenAsync(Guid id, Guid userId, CancellationToken ct);
	Task<TelegramBotDto?> GetTelegramBotByScheduleIdAsync(Guid scheduleId, Guid userId, CancellationToken ct);
	Task<TelegramBotDto?> GetTelegramBotByScheduleIdAsync(Guid scheduleId, CancellationToken ct);
	Task<TelegramBotDto?> GetTelegramBotByMessageIdAsync(Guid messageId, Guid userId, CancellationToken ct);
}
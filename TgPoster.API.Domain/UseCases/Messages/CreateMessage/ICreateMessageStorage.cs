using TgPoster.API.Domain.Services;
using TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessage;

public interface ICreateMessageStorage
{
    Task<bool> ExistScheduleAsync(Guid userId, Guid scheduleId, CancellationToken ct);
    Task<TelegramBotDto?> GetTelegramBotAsync(Guid scheduleId, Guid userId, CancellationToken ct);

    Task<Guid> CreateMessagesAsync(
        Guid scheduleId,
        string? text,
        DateTimeOffset time,
        List<MediaFileResult> files,
        CancellationToken ct
    );
}
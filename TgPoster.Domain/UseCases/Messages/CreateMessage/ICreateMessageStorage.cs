using TgPoster.Domain.Services;
using TgPoster.Domain.UseCases.Messages.CreateMessagesFromFiles;

namespace TgPoster.Domain.UseCases.Messages.CreateMessage;

public interface ICreateMessageStorage
{
    Task<bool> ExistSchedule(Guid userId, Guid scheduleId, CancellationToken ct);
    Task<TelegramBotDto?> GetTelegramBot(Guid scheduleId, Guid userId, CancellationToken ct);
    Task<Guid> CreateMessages(Guid scheduleId, string? text, DateTimeOffset time, List<MediaFileResult> files, CancellationToken ct);
}
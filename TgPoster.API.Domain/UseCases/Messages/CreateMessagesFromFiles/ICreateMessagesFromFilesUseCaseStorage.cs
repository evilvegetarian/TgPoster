using TgPoster.API.Domain.Services;

namespace TgPoster.API.Domain.UseCases.Messages.CreateMessagesFromFiles;

public interface ICreateMessagesFromFilesUseCaseStorage
{
    Task<TelegramBotDto?> GetTelegramBotAsync(Guid scheduleId, Guid userId, CancellationToken ct);
    Task<List<DateTimeOffset>> GetExistMessageTimePostingAsync(Guid scheduleId, CancellationToken ct);

    Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTimeAsync(
        Guid scheduleId,
        CancellationToken ct
    );

    Task CreateMessagesAsync(
        Guid requestScheduleId,
        List<MediaFileResult> files,
        List<DateTimeOffset> postingTime,
        CancellationToken ct
    );
}
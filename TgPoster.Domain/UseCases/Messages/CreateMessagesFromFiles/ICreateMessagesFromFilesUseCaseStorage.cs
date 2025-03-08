using TgPoster.Domain.Services;

namespace TgPoster.Domain.UseCases.Messages.CreateMessagesFromFiles;

public interface ICreateMessagesFromFilesUseCaseStorage
{
    Task<TelegramBotDto?> GetTelegramBot(Guid scheduleId, Guid userId, CancellationToken cancellationToken);
    Task<List<DateTimeOffset>> GetExistMessageTimePosting(Guid scheduleId, CancellationToken cancellationToken);
    Task<Dictionary<DayOfWeek, List<TimeOnly>>> GetScheduleTime(Guid scheduleId, CancellationToken cancellationToken);
    Task CreateMessages(Guid requestScheduleId, List<MediaFileResult> files, List<DateTimeOffset> postingTime, CancellationToken cancellationToken);
}
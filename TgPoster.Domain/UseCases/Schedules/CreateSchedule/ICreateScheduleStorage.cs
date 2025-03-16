namespace TgPoster.Domain.UseCases.Schedules.CreateSchedule;

public interface ICreateScheduleStorage
{
    Task<Guid> CreateSchedule(
        string name,
        Guid userId,
        Guid telegramBot,
        long channelId,
        CancellationToken cancellationToken
    );

    Task<string?> GetApiToken(Guid telegramBotId, Guid userId, CancellationToken cancellationToken);
}
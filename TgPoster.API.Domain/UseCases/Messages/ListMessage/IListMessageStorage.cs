namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public interface IListMessageStorage
{
    Task<bool> ExistSchedule(Guid scheduleId, CancellationToken cancellationToken);
    Task<string?> GetApiToken(Guid scheduleId, CancellationToken cancellationToken);
    Task<List<MessageDto>> GetMessagesAsync(Guid scheduleId, CancellationToken cancellationToken);
}
namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public interface IListMessageStorage
{
    Task<bool> ExistScheduleAsync(Guid scheduleId, CancellationToken cancellationToken);
    Task<string?> GetApiTokenAsync(Guid scheduleId, CancellationToken cancellationToken);
    Task<List<MessageDto>> GetMessagesAsync(Guid scheduleId, CancellationToken cancellationToken);
}
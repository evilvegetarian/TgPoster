namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public interface IListMessageStorage
{
    Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ct);
    Task<string?> GetApiTokenAsync(Guid scheduleId, CancellationToken ct);
    Task<List<MessageDto>> GetMessagesAsync(Guid scheduleId, CancellationToken ct);
}
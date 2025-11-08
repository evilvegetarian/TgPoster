using TgPoster.API.Domain.Models;

namespace TgPoster.API.Domain.UseCases.Messages.ListMessage;

public interface IListMessageStorage
{
	Task<bool> ExistScheduleAsync(Guid scheduleId, Guid userId, CancellationToken ct);
	Task<string?> GetApiTokenAsync(Guid scheduleId, CancellationToken ct);
	Task<PagedList<MessageDto>> GetMessagesAsync(ListMessageQuery query, CancellationToken pageNumber);
}
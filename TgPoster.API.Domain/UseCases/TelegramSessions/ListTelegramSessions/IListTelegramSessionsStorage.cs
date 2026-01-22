namespace TgPoster.API.Domain.UseCases.TelegramSessions.ListTelegramSessions;

public interface IListTelegramSessionsStorage
{
	Task<List<TelegramSessionResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
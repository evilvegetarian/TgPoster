namespace TgPoster.API.Domain.UseCases.TelegramSessions.DeleteTelegramSession;

public interface IDeleteTelegramSessionStorage
{
	Task<bool> ExistsAsync(Guid userId, Guid sessionId, CancellationToken ct);
	Task DeleteAsync(Guid sessionId, CancellationToken ct);
}

namespace TgPoster.API.Domain.UseCases.TelegramSessions.StartAuth;

public interface IStartAuthStorage
{
	Task<bool> ExistsAsync(Guid userId, Guid sessionId, CancellationToken ct);
}
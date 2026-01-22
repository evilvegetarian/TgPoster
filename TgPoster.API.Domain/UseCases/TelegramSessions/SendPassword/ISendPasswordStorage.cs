namespace TgPoster.API.Domain.UseCases.TelegramSessions.SendPassword;

public interface ISendPasswordStorage
{
	Task<bool> ExistsAsync(Guid userId, Guid sessionId, CancellationToken ct);
}
namespace TgPoster.API.Domain.UseCases.TelegramSessions.VerifyCode;

public interface IVerifyCodeStorage
{
	Task<bool> ExistsAsync(Guid userId, Guid sessionId, CancellationToken ct);
}

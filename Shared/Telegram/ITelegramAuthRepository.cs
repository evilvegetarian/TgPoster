namespace Shared.Telegram;

/// <summary>
///     Репозиторий для работы с Telegram авторизацией.
/// </summary>
public interface ITelegramAuthRepository
{
	Task<TelegramSessionDto?> GetByIdAsync(Guid sessionId, CancellationToken ct);
	Task UpdateSessionDataAsync(Guid sessionId, string sessionData, CancellationToken ct);
	Task UpdateStatusAsync(Guid sessionId, TelegramSessionStatus status, CancellationToken ct);
}
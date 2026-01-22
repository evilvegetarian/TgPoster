namespace Shared.Telegram;

/// <summary>
///     Репозиторий для работы с Telegram сессиями.
/// </summary>
public interface ITelegramSessionRepository
{
	Task<TelegramSessionDto?> GetByIdAsync(Guid sessionId, CancellationToken ct);
}
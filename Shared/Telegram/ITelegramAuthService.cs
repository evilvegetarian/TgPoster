using WTelegram;

namespace Shared.Telegram;

/// <summary>
///     Интерфейс для управления авторизацией Telegram сессий.
/// </summary>
public interface ITelegramAuthService
{
	Task<Client> GetClientAsync(Guid sessionId, CancellationToken ct = default);
	Task RemoveClientAsync(Guid sessionId);
	Task<string> StartAuthAsync(Guid sessionId, CancellationToken ct);
	Task<VerifyCodeResult> VerifyCodeAsync(Guid sessionId, string code, CancellationToken ct);
	Task<bool> SendPasswordAsync(Guid sessionId, string password, CancellationToken ct);
}

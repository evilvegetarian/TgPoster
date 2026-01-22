namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое при неверном коде верификации Telegram.
/// </summary>
public sealed class TelegramInvalidVerificationCodeException(Guid sessionId, string? details = null)
	: SharedException($"Неверный код верификации для сессии {sessionId}" + (details != null ? $": {details}" : string.Empty));

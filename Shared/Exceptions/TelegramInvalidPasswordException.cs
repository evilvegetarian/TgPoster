namespace Shared.Exceptions;

/// <summary>
///     Исключение, выбрасываемое при неверном пароле двухфакторной аутентификации Telegram.
/// </summary>
public sealed class TelegramInvalidPasswordException(Guid sessionId, string? details = null)
	: SharedException($"Неверный пароль 2FA для сессии {sessionId}"
	                  + (details != null ? $": {details}" : string.Empty));
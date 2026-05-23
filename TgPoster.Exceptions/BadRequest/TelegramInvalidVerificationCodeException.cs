namespace TgPoster.Exceptions;

/// <summary>
///     Исключение, выбрасываемое при неверном коде верификации Telegram
/// </summary>
/// <param name="sessionId">Идентификатор Telegram-сессии</param>
/// <param name="details">Дополнительные детали ошибки</param>
public sealed class TelegramInvalidVerificationCodeException(Guid sessionId, string? details = null)
	: DomainException($"Неверный код верификации для сессии {sessionId}"
	                  + (details != null ? $": {details}" : string.Empty));

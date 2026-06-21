using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое при неверном пароле двухфакторной аутентификации Telegram
/// </summary>
/// <param name="sessionId">Идентификатор Telegram-сессии</param>
/// <param name="details">Дополнительные детали ошибки</param>
public sealed class TelegramInvalidPasswordException(Guid sessionId, string? details = null)
	: DomainException($"Неверный пароль 2FA для сессии {sessionId}"
	                  + (details != null ? $": {details}" : string.Empty));
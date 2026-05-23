using TgPoster.Exceptions.Base;

namespace TgPoster.Exceptions.BadRequest;

/// <summary>
///     Исключение, выбрасываемое когда Telegram отклоняет ключ авторизации сессии (AUTH_KEY_DUPLICATED).
///     Сессия необратимо инвалидирована и требует полной повторной авторизации
/// </summary>
/// <param name="sessionId">Идентификатор Telegram-сессии</param>
/// <param name="innerException">Исходное исключение</param>
public sealed class TelegramAuthKeyDuplicatedException(Guid sessionId, Exception? innerException = null)
    : DomainException($"Ключ авторизации дублирован для сессии {sessionId}. Сессия деактивирована.")
{
    public Guid SessionId { get; } = sessionId;

    public new Exception? InnerException { get; } = innerException;
}
